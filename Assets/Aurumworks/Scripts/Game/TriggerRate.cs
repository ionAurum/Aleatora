using System;

namespace Aurumworks.Aleatora
{
    /// <summary>
    /// The right trigger's rate (EM-6, after Jon's try; ruling 80): how far the trigger is pulled
    /// sweeps the spray's rate from the wheel's least (a light pull) up to Rate (a full pull), on the
    /// wheel's geometric curve, so each bit of travel is the same felt change, as each notch is.
    /// A pull inside the dead zone sprays nothing. A Rate at or below the least is reached linearly.
    /// </summary>
    public static class TriggerRate
    {
        /// <summary>A pull at or below this (0 to 1) sprays nothing: a finger resting on the trigger.</summary>
        public const double DeadZone = 0.02;

        /// <summary>Spheres a second for a pull of 0 to 1, between least (just past the dead zone) and rate (a full pull).</summary>
        public static double Rate(double pull, double least, double rate)
        {
            if (double.IsNaN(pull) || pull <= DeadZone || rate <= 0.0)
            {
                return 0.0;
            }

            double travel = Math.Min(1.0, (pull - DeadZone) / (1.0 - DeadZone));
            if (least <= 0.0 || rate <= least)
            {
                return rate * travel;
            }

            return least * Math.Pow(rate / least, travel);
        }
    }
}
