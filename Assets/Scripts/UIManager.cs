using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum charEmotion
{
    happy,
    normal,
    sad,
    mad,
    menhara,
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private Image charProfile;
    
    [System.Serializable]
    public struct ImageSet
    {
        public charEmotion emotion;
        public Sprite image;
    }
    public List<ImageSet> charImage;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    public void CompleteMap()
    {
        foreach (ImageSet set in charImage)
        {
            if (set.emotion == charEmotion.happy)
            {
                charProfile.sprite = set.image;
            }
        }
    }
}
