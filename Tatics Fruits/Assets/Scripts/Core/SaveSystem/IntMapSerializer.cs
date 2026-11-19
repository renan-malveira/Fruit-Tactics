using System.Collections.Generic;

namespace Core.SaveSystem
{
    /// <summary>
    /// O JsonUtility (usado no save local e no Firebase) ignora Dictionary.
    /// As classes de save guardam o mapa em duas listas paralelas (chaves e valores)
    /// e usam estes métodos em OnBeforeSerialize / OnAfterDeserialize.
    /// </summary>
    public static class IntMapSerializer
    {
        public static void Write(Dictionary<string, int> map, List<string> keys, List<int> values)
        {
            keys.Clear();
            values.Clear();
            if (map == null) return;

            foreach (var kv in map)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        public static Dictionary<string, int> Read(List<string> keys, List<int> values)
        {
            var map = new Dictionary<string, int>();
            if (keys == null || values == null) return map;

            var count = keys.Count < values.Count ? keys.Count : values.Count;
            for (var i = 0; i < count; i++)
            {
                var key = keys[i];
                if (string.IsNullOrEmpty(key)) continue;

                // Chave repetida: fica o maior valor (recorde e estrelas só sobem).
                if (!map.TryGetValue(key, out var current) || values[i] > current)
                    map[key] = values[i];
            }
            return map;
        }

        /// <summary>Junta dois mapas ficando com o maior valor de cada chave.</summary>
        public static Dictionary<string, int> MergeMax(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            var result = a != null ? new Dictionary<string, int>(a) : new Dictionary<string, int>();
            if (b == null) return result;

            foreach (var kv in b)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                if (!result.TryGetValue(kv.Key, out var current) || kv.Value > current)
                    result[kv.Key] = kv.Value;
            }
            return result;
        }
    }
}
