using UnityEngine;

namespace ModularChess.Match
{
    [DefaultExecutionOrder(-100)]
    public sealed class Initializer : MonoBehaviour
    {
        [SerializeField] MonoBehaviour[] steps;

        void Awake()
        {
            if (steps == null)
                return;

            for (int i = 0; i < steps.Length; i++)
            {
                MonoBehaviour step = steps[i];
                if (step == null)
                {
                    Debug.LogWarning($"Initializer step {i} is unassigned.", this);
                    continue;
                }

                if (step is IInitializable initializable)
                {
                    initializable.Initialize();
                    continue;
                }

                Debug.LogWarning(
                    $"{step.name} is in initialize order but does not implement IInitializable.",
                    step);
            }
        }
    }
}
