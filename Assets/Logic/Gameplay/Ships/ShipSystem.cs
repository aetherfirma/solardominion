using System;
using UnityEngine;

namespace Logic.Gameplay.Ships
{
    public class ShipSystem : MonoBehaviour
    {
        public SystemType Type;
        public ShipSystem[] SubSystems;
        public int Thrust;
        public int Defence;
        public int Shots, Damage, ShortRange, ShortModifier, MediumRange, MediumModifier, LongRange, LongModifier;
        public Order[] Orders;
        public GameObject[] Model;
        public bool Displayed;
        public Sprite Icon;
        public int[] Cost;
        public string Description;

        public ShipSystem ResolveSystem(int subsystem)
        {
            return Type != SystemType.Composite ? this : SubSystems[subsystem];
        }

        public string Describe()
        {
            var output = Description != ""
                ? string.Format("<b><size=15>{0}</size></b>\n{1}\n\n", name, Description)
                : string.Format("<b><size=15>{0}</size></b>\n", name);
            switch (Type)
            {
                case SystemType.Engine:
                    output += string.Format("Provides {0} point{1} of thrust", Thrust, Thrust == 1 ? "" : "s");
                    break;
                case SystemType.Weapon:
                    output += string.Format(
                        "Weapon firing {0} shot{1} doing {2} damage{3}.\nShort range {4} - Modifier {5}\nMedium range {6} - Modifier {7}\nLong range {8} - Modifier {9}",
                        Shots, Shots == 1 ? "" : "s", 
                        Damage, Shots == 1 ? "" : " each",
                        ShortRange, ShortModifier,
                        MediumRange, MediumModifier,
                        LongRange, LongModifier
                    );
                    break;
                case SystemType.Command:
                    output += "Can issue orders";
                    break;
                case SystemType.Hangar:
                    output += "Can hold and launch strike craft";
                    break;
                case SystemType.Defence:
                    output += string.Format("Provides a +{0} bonus to defensive rolls", Defence);
                    break;
                case SystemType.Composite:
                    output += "Is either:\n\n";
                    var first = true;
                    foreach (var subsystem in SubSystems)
                    {
                        if (first) first = false;
                        else output += "\n\nor\n\n";

                        output += subsystem.Describe();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return output;
        }
    }
}