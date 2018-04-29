using Logic.Maths;
using UnityEngine;

namespace Logic.Gameplay.Rules
{
    public class Scenario
    {
        public static Scenario Confrontation = new Scenario("Confrontation", 2, 1500, new PointInside[]
        {
            new Circle2D(new Vector2(60,60), 35), 
            new Circle2D(new Vector2(-60,-60), 35) 
        });

        public string Name;
        public int Players;
        public PointInside[] DeploymentAreas;
        public int PointsLimit;

        public Scenario(string name, int players, int pointsLimit, PointInside[] deploymentAreas)
        {
            Players = players;
            DeploymentAreas = deploymentAreas;
            PointsLimit = pointsLimit;
            Name = name;
        }
    }
}