using CreatureLevelControl;
using System.Collections.Generic;

namespace AllTameable.CLLC
{
    internal class CLLC
    {
        
        public static int Hatchlevel(Character character)
        {
            return API.LevelRand(character);
        }

        public static bool CLLC_HasExtraEffect(Character character)
        {
            return API.HasExtraEffectCreature(character);
        }

        public static bool CLLC_HasInfusion(Character character)
        {
            return API.HasInfusionCreature(character);
        }

        public static void SetSpecial(Character character)
        {

        }

        public static bool CheckCLLC()
        {
            try
            {
                return API.IsEnabled();
            }
            catch
            {
                return false;
            }

        }

        
    }
}
