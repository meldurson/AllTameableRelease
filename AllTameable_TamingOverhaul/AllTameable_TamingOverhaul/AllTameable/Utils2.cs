using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AllTameable
{
    public static class Utils2
    {
        private const BindingFlags bindingFlags = BindingFlags.Public;

        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType())
            {
                return null;
            }
            List<Type> list = new List<Type>();
            Type baseType = type.BaseType;
            while (baseType != null && !(baseType == typeof(MonoBehaviour)))
            {
                list.Add(baseType);
                baseType = baseType.BaseType;
            }
            IEnumerable<PropertyInfo> enumerable = type.GetProperties(BindingFlags.Public);
            foreach (Type item in list)
            {
                enumerable = enumerable.Concat(item.GetProperties(BindingFlags.Public));
            }
            enumerable = from property in enumerable
                         where !(type == typeof(Rigidbody)) || !(property.Name == "inertiaTensor")
                         where !property.CustomAttributes.Any((CustomAttributeData attribute) => attribute.AttributeType == typeof(ObsoleteAttribute))
                         select property;
            foreach (PropertyInfo pinfo in enumerable)
            {
                if (pinfo.CanWrite && !enumerable.Any((PropertyInfo e) => e.Name == $"shared{char.ToUpper(pinfo.Name[0])}{pinfo.Name.Substring(1)}"))
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch
                    {
                    }
                }
            }
            IEnumerable<FieldInfo> enumerable2 = type.GetFields(BindingFlags.Public);
            foreach (FieldInfo finfo in enumerable2)
            {
                foreach (Type item2 in list)
                {
                    if (!enumerable2.Any((FieldInfo e) => e.Name == $"shared{char.ToUpper(finfo.Name[0])}{finfo.Name.Substring(1)}"))
                    {
                        enumerable2 = enumerable2.Concat(item2.GetFields(BindingFlags.Public));
                    }
                }
            }
            foreach (FieldInfo item3 in enumerable2)
            {
                item3.SetValue(comp, item3.GetValue(other));
            }
            enumerable2 = enumerable2.Where((FieldInfo field) => field.CustomAttributes.Any((CustomAttributeData attribute) => attribute.AttributeType == typeof(ObsoleteAttribute)));
            foreach (FieldInfo item4 in enumerable2)
            {
                item4.SetValue(comp, item4.GetValue(other));
            }
            return comp as T;
        }

        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent(toAdd.GetType()).GetCopyOf(toAdd);
        }

        public static T CopyBroComponet<T, TU>(this Component comp, TU other) where T : Component
        {
            Type baseType = comp.GetType().BaseType;
            IEnumerable<FieldInfo> fields = baseType.GetFields(BindingFlags.Public);
            foreach (FieldInfo item in fields)
            {
                item.SetValue(comp, item.GetValue(other));
                DBG.blogWarning(item?.ToString() + " , " + item.GetType()?.ToString() + " , " + item.Name);
            }
            return comp as T;
        }

        public static List<T> RemoveList<TU, T>(this List<T> instance, Dictionary<TU, T> other)
        {
            foreach (T value in other.Values)
            {
                if (instance.Contains(value))
                {
                    instance.Remove(value);
                }
            }
            return instance;
        }

        public static Dictionary<T, TU> RemoveList<T, TU>(this Dictionary<T, TU> instance, Dictionary<T, TU> other)
        {
            foreach (T key in other.Keys)
            {
                if (instance.ContainsKey(key))
                {
                    instance.Remove(key);
                }
            }
            return instance;
        }

        public static int LevelFromProb(float[] probabilities)
        {
            int num = UnityEngine.Random.Range(0, 100);

            int index = 0;
            float sum = 0;
            while (sum <= num & index < probabilities.Length)
            {
                sum += probabilities[index];
                index++;
            }
            DBG.blogDebug("Level=" + (index) + " : num=" + num + " :sum=" + sum);
            return index;
        }

        public static void addOrUpdateCustomData(Dictionary<string, string> dic, string key, string newValue)
        {
            if (dic.ContainsKey(key))
            {
                DBG.blogDebug("Already had "+key);
                dic[key] = newValue;
            }
            else
            {
                DBG.blogDebug("Added "+key+" to dictionary");
                dic.Add(key, newValue);
            }
        }

        public static Color colFromHex(string hexStr)
        {
            try
            {
                DBG.blogDebug("in colfromhex");
                int intval = int.Parse(hexStr.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
                float b = (intval >> 0) & 255;
                float g = (intval >> 8) & 255;
                float r = (intval >> 16) & 255;
                DBG.blogDebug("r,g,b=" + r + "," + g + "," + b);
                Color col = new Color(r/255 , g/255 , b/255 );

                return col;
            }
            catch
            {
                DBG.blogWarning("Not a valid hex color code");

                return Color.white;
            }
            
        }

        public static Texture2D changeEggTex(Texture2D oldTex, Color col, bool invertShadow)
        {
            DBG.blogDebug("in change color");
            Texture2D newTex = UnityEngine.Object.Instantiate(oldTex);

            // Create the colors to use
            Color[] colors = new Color[3];
            colors[0] = col;
            colors[1] = col;
            colors[2] = col;
            int mipCount = Mathf.Min(3, newTex.mipmapCount);
            //DBG.blogDebug("mipcount =" + mipCount);
            // For each mipmap level, use GetPixels to fetch an array of pixel data, and use SetPixels to fill the mipmap level with one color.
            for (int mip = 0; mip < mipCount; ++mip)
            {
                Color[] cols = newTex.GetPixels(mip);
                DBG.blogDebug("cols.length=" + cols.Length);
                
                for (int i = 0; i < cols.Length; ++i)
                {
                    Color tempCol = cols[i];
                    if (invertShadow)
                    {
                        Color invCola = new Color(1 - tempCol.r, 1 - tempCol.g, 1 - tempCol.b);
                        Color invCol = invCola - new Color(Mathf.Min(invCola.r, 0.6f), Mathf.Min(invCola.g, 0.6f), Mathf.Min(invCola.b, 0.6f));
                        Color BoostedCol = (cols[i] * col);
                        //cols[i] = BoostedCol * tempCol - (BoostedCol * invCol * 0.7f);// + (changeHue(BoostedCol, 180f)*invCol*1.5f) - DCol;
                        cols[i] = BoostedCol - (BoostedCol * invCol * 1f);
                        cols[i] = (changeHue(BoostedCol, 0.5f) * invCola * 1.3f) + cols[i];
                    }
                    else
                    {
                        Color DCol = Color.grey * tempCol;
                        DCol = DCol - new Color(Mathf.Min(DCol.r, 0.25f), Mathf.Min(DCol.g, 0.25f), Mathf.Min(DCol.b, 0.25f));
                        cols[i] = (cols[i] * col);// - DCol;
                    }
                    
                    
                    cols[i].a = tempCol.a;
                    
                }
                newTex.SetPixels(cols, mip);
            }
            // Copy the changes to the GPU. and don't recalculate mipmap levels.
            newTex.Apply(false);
            return newTex;
        }

        public static Color changeHue(Color col, float hue)
        {
            Color.RGBToHSV(col, out float H, out float S, out float V);
            //DBG.blogDebug("H" + H + ", S=" + S + ", V=" + V);
            H = (H + hue) % 1f;
            Color returnCol = Color.HSVToRGB(H, S, V);
            
            return returnCol;
        }
    }

    
}
