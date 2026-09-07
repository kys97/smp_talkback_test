using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NamnyeoChilse
{
    public static class ResourceNameLoader
    {
        public const string ResourcePath = "Names";

        public static NameData[] Load()
        {
            NameData[] names = CreateNames(Resources.LoadAll<AudioClip>(ResourcePath));
            if (names.Length == 0)
                Debug.LogWarning("Resources/Names에 유효한 이름 음성이 없습니다. 음성 없는 기본 이름으로 진행합니다.");
            return names;
        }

        public static NameData[] CreateNames(IEnumerable<AudioClip> clips)
        {
            var names = new List<NameData>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (clips == null) return names.ToArray();

            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;
                // Unity's clip.name excludes the file extension. Normalize Hangul
                // so composed/decomposed filenames cannot register the same name twice.
                string name = clip.name.Normalize(NormalizationForm.FormC);
                Gender gender;
                if (name.EndsWith("수", StringComparison.Ordinal)) gender = Gender.Male;
                else if (name.EndsWith("희", StringComparison.Ordinal)) gender = Gender.Female;
                else
                {
                    Debug.LogWarning($"이름 음성 '{clip.name}' 제외: 파일명은 수 또는 희로 끝나야 합니다.", clip);
                    continue;
                }

                if (!seen.Add(name))
                {
                    Debug.LogWarning($"중복 이름 음성 '{name}' 제외: 먼저 로드된 클립 하나만 사용합니다.", clip);
                    continue;
                }

                names.Add(new NameData(name, gender, clip));
            }

            names.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return names.ToArray();
        }
    }
}
