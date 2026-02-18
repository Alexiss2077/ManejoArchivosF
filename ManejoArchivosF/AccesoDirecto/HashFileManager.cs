using System.Text;

namespace ManejoArchivosF.AccesoDirecto
{
    /// <summary>
    /// Motor de acceso directo real: hash + sondeo lineal + FileStream.Seek().
    ///
    /// Formato binario (256 bytes fijos por slot):
    ///   HEADER  8 bytes  → [int magic | int totalSlots]
    ///   SLOT  256 bytes  → [byte status | int id | byte[250] contenido | byte padding]
    ///
    /// Status: 0 = VACÍO  |  1 = OCUPADO  |  2 = ELIMINADO (tombstone)
    ///
    /// Se usa fs.ReadExactly() directo al buffer para evitar que el caché
    /// interno de BinaryReader se desincronice con los saltos de Seek().
    /// </summary>
    internal sealed class HashFileManager
    {
        private const int MAGIC = 0x46524144;   // "DARF"
        private const int HEADER_SIZE = 8;
        private const int RECORD_SIZE = 256;
        private const int CONTENIDO_MAX = 250;

        public const byte EMPTY = 0;
        public const byte OCCUPIED = 1;
        public const byte DELETED = 2;

        private readonly string _filePath;
        public int TotalSlots { get; }

        public HashFileManager(string filePath)
        {
            _filePath = filePath;
            var (valid, slots) = ReadHeader(filePath);
            if (!valid)
                throw new InvalidDataException("El archivo no es un .dat de acceso directo válido.");
            TotalSlots = slots;
        }

        public int Hash(int id) => Math.Abs(id % TotalSlots);

        private static long SlotOffset(int slot) => HEADER_SIZE + (long)slot * RECORD_SIZE;

        // ── Crear archivo nuevo ───────────────────────────────────────────────
        public static void CreateFile(string filePath, int totalSlots = 101)
        {
            if (totalSlots < 1) throw new ArgumentOutOfRangeException(nameof(totalSlots));

            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
            fs.Write(BitConverter.GetBytes(MAGIC), 0, 4);
            fs.Write(BitConverter.GetBytes(totalSlots), 0, 4);

            byte[] emptySlot = new byte[RECORD_SIZE];
            for (int i = 0; i < totalSlots; i++)
                fs.Write(emptySlot, 0, RECORD_SIZE);
        }

        // ── Leer encabezado ───────────────────────────────────────────────────
        public static (bool valid, int totalSlots) ReadHeader(string filePath)
        {
            if (!File.Exists(filePath)) return (false, 0);
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            if (fs.Length < HEADER_SIZE) return (false, 0);
            byte[] buf = new byte[8];
            fs.ReadExactly(buf, 0, 8);
            int magic = BitConverter.ToInt32(buf, 0);
            int slots = BitConverter.ToInt32(buf, 4);
            return (magic == MAGIC && slots > 0, slots);
        }

        // ── Leer slot completo (256 bytes de una vez — evita caché de BinaryReader) ──
        private (byte status, int id, string contenido) ReadSlot(FileStream fs, int slot)
        {
            fs.Seek(SlotOffset(slot), SeekOrigin.Begin);
            byte[] buf = new byte[RECORD_SIZE];
            fs.ReadExactly(buf, 0, RECORD_SIZE);

            byte status = buf[0];
            int id = BitConverter.ToInt32(buf, 1);
            string contenido = Encoding.UTF8.GetString(buf, 5, CONTENIDO_MAX).TrimEnd('\0');
            return (status, id, contenido);
        }

        // ── Escribir slot completo ────────────────────────────────────────────
        private static void WriteSlot(FileStream fs, int slot, byte status, int id, string contenido)
        {
            byte[] buf = new byte[RECORD_SIZE];
            buf[0] = status;
            BitConverter.GetBytes(id).CopyTo(buf, 1);
            byte[] encoded = Encoding.UTF8.GetBytes(contenido);
            Array.Copy(encoded, 0, buf, 5, Math.Min(encoded.Length, CONTENIDO_MAX));
            fs.Seek(SlotOffset(slot), SeekOrigin.Begin);
            fs.Write(buf, 0, RECORD_SIZE);
        }

        private static byte ReadSlotStatus(FileStream fs, int slot)
        {
            fs.Seek(SlotOffset(slot), SeekOrigin.Begin);
            int b = fs.ReadByte();
            return b < 0 ? EMPTY : (byte)b;
        }

        // ── Insertar → retorna slot, -1 duplicado, -2 lleno ──────────────────
        public int Insert(int id, string contenido)
        {
            using FileStream fs = new(_filePath, FileMode.Open, FileAccess.ReadWrite);
            int startSlot = Hash(id), firstDeleted = -1;

            for (int i = 0; i < TotalSlots; i++)
            {
                int slot = (startSlot + i) % TotalSlots;
                var (status, slotId, _) = ReadSlot(fs, slot);

                if (status == OCCUPIED) { if (slotId == id) return -1; }
                else if (status == DELETED) { if (firstDeleted == -1) firstDeleted = slot; }
                else
                {
                    int dest = firstDeleted != -1 ? firstDeleted : slot;
                    WriteSlot(fs, dest, OCCUPIED, id, contenido);
                    return dest;
                }
            }
            if (firstDeleted != -1) { WriteSlot(fs, firstDeleted, OCCUPIED, id, contenido); return firstDeleted; }
            return -2;
        }

        // ── Buscar ────────────────────────────────────────────────────────────
        public (RegistroDirecto? registro, int slot) Search(int id)
        {
            using FileStream fs = new(_filePath, FileMode.Open, FileAccess.Read);
            int startSlot = Hash(id);

            for (int i = 0; i < TotalSlots; i++)
            {
                int slot = (startSlot + i) % TotalSlots;
                var (status, slotId, contenido) = ReadSlot(fs, slot);

                if (status == EMPTY) break;
                if (status == OCCUPIED && slotId == id)
                    return (new RegistroDirecto { Id = id, Contenido = contenido }, slot);
            }
            return (null, -1);
        }

        // ── Actualizar ────────────────────────────────────────────────────────
        public bool Update(int id, string newContenido)
        {
            using FileStream fs = new(_filePath, FileMode.Open, FileAccess.ReadWrite);
            int startSlot = Hash(id);

            for (int i = 0; i < TotalSlots; i++)
            {
                int slot = (startSlot + i) % TotalSlots;
                var (status, slotId, _) = ReadSlot(fs, slot);

                if (status == EMPTY) return false;
                if (status == OCCUPIED && slotId == id)
                {
                    WriteSlot(fs, slot, OCCUPIED, id, newContenido);
                    return true;
                }
            }
            return false;
        }

        // ── Eliminar (tombstone) — solo cambia 1 byte en disco ────────────────
        public bool Delete(int id)
        {
            using FileStream fs = new(_filePath, FileMode.Open, FileAccess.ReadWrite);
            int startSlot = Hash(id);

            for (int i = 0; i < TotalSlots; i++)
            {
                int slot = (startSlot + i) % TotalSlots;
                var (status, slotId, _) = ReadSlot(fs, slot);

                if (status == EMPTY) return false;
                if (status == OCCUPIED && slotId == id)
                {
                    fs.Seek(SlotOffset(slot), SeekOrigin.Begin);
                    fs.WriteByte(DELETED);
                    return true;
                }
            }
            return false;
        }

        // ── Leer todos los registros OCUPADOS ─────────────────────────────────
        public List<RegistroDirecto> ReadAll()
        {
            List<RegistroDirecto> lista = new();
            using FileStream fs = new(_filePath, FileMode.Open, FileAccess.Read);

            for (int slot = 0; slot < TotalSlots; slot++)
            {
                var (status, id, contenido) = ReadSlot(fs, slot);
                if (status == OCCUPIED)
                    lista.Add(new RegistroDirecto { Id = id, Contenido = contenido });
            }
            return lista.OrderBy(r => r.Id).ToList();
        }

        // ── Estadísticas del archivo ──────────────────────────────────────────
        public (int ocupados, int eliminados, int vacios) GetStats()
        {
            int ocu = 0, del = 0, vac = 0;
            using FileStream fs = new(_filePath, FileMode.Open, FileAccess.Read);

            for (int slot = 0; slot < TotalSlots; slot++)
            {
                byte s = ReadSlotStatus(fs, slot);
                if (s == OCCUPIED) ocu++;
                else if (s == DELETED) del++;
                else vac++;
            }
            return (ocu, del, vac);
        }
    }

    internal sealed class RegistroDirecto
    {
        public int Id { get; set; }
        public string Contenido { get; set; } = string.Empty;
    }
}
