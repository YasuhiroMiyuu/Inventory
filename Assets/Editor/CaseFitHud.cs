using TMPro;
using UnityEngine;

namespace CaseFit
{
    public class CaseFitHud : MonoBehaviour
    {
        [Header("Runner")]
        [SerializeField] LevelRunner runner;

        [Header("Labels")]
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text movesLabel;
        [SerializeField] TMP_Text remainingLabel;
        [SerializeField] TMP_Text messageLabel;

        [Header("Time Warning")]
        [SerializeField] float warningSeconds = 10f;
        [SerializeField] Color normalTimeColor = Color.white;
        [SerializeField] Color warningTimeColor = new(0.85f, 0.28f, 0.18f);

        void OnEnable()
        {
            if (runner == null)
            {
                Debug.LogError("[Case Fit] CaseFitHud: field 'Runner' is empty.", this);
                return;
            }

            runner.onLevelLoaded.AddListener(HandleLevelLoaded);
            runner.onTimeChanged.AddListener(HandleTime);
            runner.onMovesChanged.AddListener(HandleMoves);
            runner.onRemainingChanged.AddListener(HandleRemaining);
            runner.onLevelCleared.AddListener(HandleCleared);
            runner.onLevelFailed.AddListener(HandleFailed);
            runner.onStuck.AddListener(HandleStuck);
        }

        void OnDisable()
        {
            if (runner == null) return;

            runner.onLevelLoaded.RemoveListener(HandleLevelLoaded);
            runner.onTimeChanged.RemoveListener(HandleTime);
            runner.onMovesChanged.RemoveListener(HandleMoves);
            runner.onRemainingChanged.RemoveListener(HandleRemaining);
            runner.onLevelCleared.RemoveListener(HandleCleared);
            runner.onLevelFailed.RemoveListener(HandleFailed);
            runner.onStuck.RemoveListener(HandleStuck);
        }

        void HandleLevelLoaded(LevelDefinition level)
        {
            Set(titleLabel, level.title);
            Set(messageLabel, level.brief);
        }

        void HandleTime(float seconds)
        {
            if (timeLabel == null) return;

            if (runner.ChillMode)
            {
                timeLabel.text = "--:--";
                timeLabel.color = normalTimeColor;
                return;
            }

            int whole = Mathf.Max(0, Mathf.CeilToInt(seconds));
            timeLabel.text = (whole / 60) + ":" + (whole % 60).ToString("00");
            timeLabel.color = whole <= warningSeconds ? warningTimeColor : normalTimeColor;
        }

        void HandleMoves(int moves) => Set(movesLabel, moves.ToString());

        void HandleRemaining(int remaining) => Set(remainingLabel, remaining.ToString());

        void HandleCleared(int stars)
        {
            Set(messageLabel, "LEVEL CLEAR    " + stars + " / 3 STARS");
        }

        void HandleFailed() => Set(messageLabel, "TIME UP");

        void HandleStuck() => Set(messageLabel, "No room left - press CLEAR CASE and try again");

        static void Set(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
