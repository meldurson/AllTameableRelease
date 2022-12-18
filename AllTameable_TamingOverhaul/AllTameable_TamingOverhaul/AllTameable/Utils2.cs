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
    }
}
