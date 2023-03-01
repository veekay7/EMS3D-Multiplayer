﻿using E3D;
using System.Collections.Generic;
using UnityEngine;

public class VictimPortraitList : ScriptableObject
{
    public Sprite m_NullMaleSprite;
    public Sprite m_NullFemaleSprite;

    public List<Sprite> m_Female_JS = new List<Sprite>();
    public List<Sprite> m_Female_JCJK = new List<Sprite>();
    public List<Sprite> m_Female_Adult = new List<Sprite>();
    public List<Sprite> m_Female_Elderly = new List<Sprite>();

    public List<Sprite> m_Male_JS = new List<Sprite>();
    public List<Sprite> m_Male_JCJK = new List<Sprite>();
    public List<Sprite> m_Male_Adult = new List<Sprite>();
    public List<Sprite> m_Male_Elderly = new List<Sprite>();


    /// <summary>
    /// Note that age range cannot be larger than 5.
    /// </summary>
    /// <returns></returns>
    public Sprite GetFemalePortraitByAgeRange(int age, out int index)
    {
        index = -1;

        if (age >= 5 && age <= 15)
        {
            index = UnityEngine.Random.Range(0, m_Female_JS.Count - 1);
            return m_Female_JS[index];
        }
        else if (age >= 16 && age <= 25)
        {
            index = UnityEngine.Random.Range(0, m_Female_JCJK.Count - 1);
            return m_Female_JCJK[index];
        }
        else if (age >= 26 && age <= 35)
        {
            index = UnityEngine.Random.Range(0, m_Female_JCJK.Count - 1);
            return m_Female_JCJK[index];
        }
        else if (age >= 36 && age <= 45)
        {
            index = UnityEngine.Random.Range(0, m_Female_Adult.Count - 1);
            return m_Female_Adult[index];
        }
        else if (age >= 46 && age <= 55)
        {
            index = UnityEngine.Random.Range(0, m_Female_Adult.Count - 1);
            return m_Female_Adult[index];
        }

        return m_NullFemaleSprite;
    }

    public Sprite GetMalePortraitByAgeRange(int age, out int index)
    {
        index = -1;

        if (age >= 5 && age <= 15)
        {
            index = UnityEngine.Random.Range(0, m_Male_JS.Count - 1);
            return m_Male_JS[index];
        }
        else if (age >= 16 && age <= 25)
        {
            index = UnityEngine.Random.Range(0, m_Male_JCJK.Count - 1);
            return m_Male_JCJK[index];
        }
        else if (age >= 26 && age <= 35)
        {
            index = UnityEngine.Random.Range(0, m_Male_JCJK.Count - 1);
            return m_Male_JCJK[index];
        }
        else if (age >= 36 && age <= 45)
        {
            index = UnityEngine.Random.Range(0, m_Male_Adult.Count - 1);
            return m_Male_Adult[index];
        }
        else if (age >= 46 && age <= 55)
        {
            index = UnityEngine.Random.Range(0, m_Male_Adult.Count - 1);
            return m_Male_Adult[index];
        }

        return m_NullMaleSprite;
    }

    public Sprite GetPortraitByIndex(ESex sex, int age, int index)
    {
        if (index > -1)
        {
            if (sex == ESex.Female)
            {
                if (age >= 5 && age <= 15)
                {
                    return m_Female_JS[index];
                }
                else if (age >= 16 && age <= 25)
                {
                    return m_Female_JCJK[index];
                }
                else if (age >= 26 && age <= 35)
                {
                    return m_Female_JCJK[index];
                }
                else if (age >= 36 && age <= 45)
                {
                    return m_Female_Adult[index];
                }
                else if (age >= 46 && age <= 55)
                {
                    return m_Female_Adult[index];
                }
            }
            else if (sex == ESex.Male)
            {
                if (age >= 5 && age <= 15)
                {
                    return m_Male_JS[index];
                }
                else if (age >= 16 && age <= 25)
                {
                    return m_Male_JCJK[index];
                }
                else if (age >= 26 && age <= 35)
                {
                    return m_Male_JCJK[index];
                }
                else if (age >= 36 && age <= 45)
                {
                    return m_Male_Adult[index];
                }
                else if (age >= 46 && age <= 55)
                {
                    return m_Male_Adult[index];
                }
            }
        }

        return sex == ESex.Male ? m_NullMaleSprite : m_NullFemaleSprite; 
    }
}
