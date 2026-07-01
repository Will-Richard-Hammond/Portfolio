using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace OpenGL_Game.GameRefactor.Services
{
    class PowerUpService
    {
        public void UpdatePowerUps<TPowerUpState>(
            IEnumerable<TPowerUpState> powerUps,
            Vector3 playerPosition,
            Func<TPowerUpState, bool> isCollected,
            Action<TPowerUpState> markCollected,
            Func<TPowerUpState, string> getEntityName,
            Func<TPowerUpState, object> getType,
            Func<string, Vector3?> getPosition,
            Action<string, Vector3> setPosition,
            Action<object> applyPowerUp)
        {
            foreach (var powerUp in powerUps)
            {
                if (isCollected(powerUp))
                    continue;

                string entityName = getEntityName(powerUp);
                var powerPos = getPosition(entityName);
                if (!powerPos.HasValue)
                    continue;

                if ((powerPos.Value - playerPosition).Length <= 0.8f)
                {
                    markCollected(powerUp);
                    setPosition(entityName, new Vector3(1000f, -1000f, 1000f));
                    applyPowerUp(getType(powerUp));
                }
            }
        }
    }
}
