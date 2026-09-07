using System;
using System.Collections.Generic;

namespace NamnyeoChilse
{
    public enum Gender { Male, Female }

    // Selection returns the entire entry, including its optional audio reference.
    public sealed class NameManager
    {
        private readonly List<NameData> maleNames = new List<NameData>();
        private readonly List<NameData> femaleNames = new List<NameData>();
        private readonly Random random;

        public NameManager() : this(new Random()) { }

        public NameManager(Random random) : this(CreateDefaultNames(), random) { }

        public NameManager(IEnumerable<NameData> entries, Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (entries != null)
            {
                foreach (NameData entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Name)) continue;
                    if (entry.Gender != Gender.Male && entry.Gender != Gender.Female) continue;
                    if (!seen.Add(entry.Name)) continue;
                    if (entry.Gender == Gender.Male) maleNames.Add(entry);
                    else if (entry.Gender == Gender.Female) femaleNames.Add(entry);
                }
            }

            // Missing or invalid resource clips must not make starting a round fail.
            if (maleNames.Count == 0 && femaleNames.Count == 0)
            {
                foreach (NameData entry in CreateDefaultNames())
                    (entry.Gender == Gender.Male ? maleNames : femaleNames).Add(entry);
            }
        }

        public NameData Next()
        {
            // Preserve the original 50/50 gender choice when both groups exist.
            List<NameData> names = maleNames.Count == 0 ? femaleNames
                : femaleNames.Count == 0 ? maleNames
                : random.Next(2) == 0 ? maleNames : femaleNames;
            return names[random.Next(names.Count)];
        }

        public NameData[] NextSequence(int requestedCount)
        {
            // Draw without replacement; a small catalog reduces the actual count.
            var males = new List<NameData>(maleNames);
            var females = new List<NameData>(femaleNames);
            int count = Math.Min(Math.Max(1, requestedCount), males.Count + females.Count);
            var result = new NameData[count];
            for (int i = 0; i < count; i++)
            {
                List<NameData> pool = males.Count == 0 ? females : females.Count == 0 ? males
                    : random.Next(2) == 0 ? males : females;
                int index = random.Next(pool.Count);
                result[i] = pool[index];
                pool.RemoveAt(index);
            }
            return result;
        }

        public static NameData[] CreateDefaultNames()
        {
            return new[]
            {
                new NameData("영수", Gender.Male), new NameData("철수", Gender.Male),
                new NameData("민수", Gender.Male), new NameData("현수", Gender.Male),
                new NameData("진수", Gender.Male), new NameData("영희", Gender.Female),
                new NameData("민희", Gender.Female), new NameData("정희", Gender.Female),
                new NameData("수희", Gender.Female), new NameData("은희", Gender.Female)
            };
        }
    }
}
