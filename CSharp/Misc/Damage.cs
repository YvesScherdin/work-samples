using MageGame.Behaviours.Combat;
using UnityEngine;

namespace MageGame.Data
{
    public class Damage
    {
        public DamageType type;
        public float amount;
        public bool critical;
        public GameObject causer;
        public Behaviour causingBehaviour;
        public bool gameTimePremultiplied;

        #region init
        public Damage() { }

        public Damage(DamageType type)
        {
            this.type = type;
        }

        public Damage(DamageType type, float amount)
        {
            this.type = type;
            this.amount = amount;
        }
        #endregion

        public override string ToString()
        {
            return "Damage " + type + " " + amount.ToString("0.0") + " " + (critical ? "!" : "");
        }

        public Damage Clone(float newDamageAmount)
        {
            Damage damage = new Damage();

            damage.type = type;
            damage.amount = newDamageAmount;
            damage.critical = critical;
            damage.causer = causer;
            damage.causingBehaviour = causingBehaviour;
            damage.gameTimePremultiplied = gameTimePremultiplied;

            return damage;
        }

        public Damage Clone()
        {
            Damage damage = new Damage();

            damage.type = type;
            damage.amount = amount;
            damage.critical = critical;
            damage.causer = causer;
            damage.causingBehaviour = causingBehaviour;
            damage.gameTimePremultiplied = gameTimePremultiplied;

            return damage;
        }

        #region factory methods
        static public Damage Generate(DamageType damageType, RangeF damage, float timeDelta, GameObject causer, MonoBehaviour causingBehaviour)
        {
            Damage dmg = new Damage(damageType, damage.GetRandomValue() * timeDelta);
            dmg.causingBehaviour = causingBehaviour;
            dmg.causer = causer;
            dmg.gameTimePremultiplied = true;
            return dmg;
        }

        static public Damage Generate(DamageType damageType, float damage, float timeDelta, GameObject causer, MonoBehaviour causingBehaviour)
        {
            Damage dmg = new Damage(damageType, damage * timeDelta);
            dmg.causingBehaviour = causingBehaviour;
            dmg.causer = causer;
            dmg.gameTimePremultiplied = true;
            return dmg;
        }
        #endregion
    }

    public enum DamageType
    {
        Fire = 0,
        Ice = 1,
        Lightning = 2,
        Wind = 3,
        Mental = 4,
        Special = 5,
        Physical = 6
    }

    public enum PhysicalDamageType
    {
        None = 0,
        Cut = 1 << 0,
        Blunt = 1 << 1,
        Thrust = 1 << 2,
        Sever = 1 << 3,
        Crunch = 1 << 4
    }

    static public class DamageExtensions
    {
        static public GameObject GetOriginalCauser(this Damage damage)
        {
            if (damage.causingBehaviour is IIntermediateCauser)
                return ((IIntermediateCauser)damage.causingBehaviour).GetOriginalCauser();
            else
                return damage.causer;
        }

        static public float GetModifiedAmountByResistance(this Damage damage, float resistanceValue)
        {
            float damageAmount = damage.amount;

            if (resistanceValue != 0f)
            {
                if (damage.gameTimePremultiplied)
                    resistanceValue *= Time.deltaTime;

                damageAmount -= resistanceValue;
                if (damageAmount < 0f)
                    damageAmount = 0f;
            }

            return damageAmount;
        }
    }
}