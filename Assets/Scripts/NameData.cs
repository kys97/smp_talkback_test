using System;
using UnityEngine;

namespace NamnyeoChilse
{
    [Serializable]
    public struct NameData
    {
        [SerializeField] private string name;
        [SerializeField] private Gender gender;
        [Tooltip("이름 음성. 비워 두어도 게임을 진행할 수 있습니다.")]
        [SerializeField] private AudioClip audioClip;

        public string Name => name;
        public Gender Gender => gender;
        public AudioClip AudioClip => audioClip;

        public NameData(string name, Gender gender, AudioClip audioClip = null)
        {
            this.name = name;
            this.gender = gender;
            this.audioClip = audioClip;
        }
    }
}
