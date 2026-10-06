using UnityEngine;

namespace SecretOfHegra
{
    /// <summary>
    /// Stop 3 — Theater Square (an imagined square, not a known Hegra monument). Maliku introduces the play
    /// and the Moon in it points beyond the stage to a stone elephant (inspired by AlUla's Jabal AlFil). The lamp
    /// uncovers letters on its face; repeating the three glowing glyphs in order grants Sign 1.
    /// The letters are revealable from the moment the player arrives, so skipping the play never blocks it.
    /// The old wall behind the seats is now scenery only and grants nothing.
    /// </summary>
    public class TheaterStop : StopBase
    {
        public NpcFigure maliku;
        public Transform stageCenter;
        public PerformanceDirector director;
        /// <summary>The lamp-reveal on the Elephant Rock's face (Sign 1).</summary>
        public LampRevealTarget inscription;
        public ElephantRockPuzzle rock;
        public float arriveDistance = 9f;
        public float rockApproachDistance = 7f;

        bool m_IntroStarted, m_ApproachToastShown;

        public override void Initialize()
        {
            if (inscription) inscription.Revealed += OnInscriptionRevealed;
            if (rock)
            {
                rock.Solved += OnRockSolved;
                rock.Mistake += OnRockMistake;
            }
        }

        public override void RestoreState(GameState s)
        {
            bool open = s.step >= QuestStep.TheaterIntro;
            if (rock) rock.Restore(s.rockGlyphsRevealed, s.signInscription);
            else if (inscription) inscription.SetRevealed(s.signInscription);
            if (inscription) inscription.active = open;
            if (rock) rock.interactive = open;
            if (director && (s.performanceSeen || s.step > QuestStep.TheaterPerformance)) director.FinalTableau();
        }

        public override void OnStepEntered(QuestStep step)
        {
            if (inscription) inscription.active = true;
            if (rock) rock.interactive = true;
            switch (step)
            {
                case QuestStep.TheaterIntro:
                    ObjectiveBeacon.Current = stageCenter;
                    m_IntroStarted = false;
                    break;

                case QuestStep.TheaterPerformance:
                    ObjectiveBeacon.Current = stageCenter;
                    Game.Objective.SetActions(("ui.skip", SkipPerformance), ("ui.replay", ReplayPerformance));
                    StartPerformance();
                    break;

                case QuestStep.TheaterFindInscription:
                    ObjectiveBeacon.Current = RockTarget;
                    m_ApproachToastShown = false;
                    if (rock && rock.Revealed && !rock.IsSolved)
                    {
                        ShowRepeatObjective();
                        rock.PlaySequence(1f);
                    }
                    else SetFindActions();
                    Game.Dialogue.Play("theater_after");
                    break;
            }
        }

        Transform RockTarget => rock && rock.face ? rock.face : inscription ? inscription.transform : null;

        void SetFindActions() => Game.Objective.SetActions(("ui.replay", ReplayPerformance));

        void ShowRepeatObjective()
        {
            Game.Objective.Show("obj.rock_repeat", "ctl.rock_repeat");
            Game.Objective.SetActions(("ui.replay_glyphs", ReplayGlyphs), ("menu.reset_puzzle", ResetPuzzle));
        }

        void ReplayGlyphs()
        {
            if (rock) rock.PlaySequence(0.2f);
        }

        void Update()
        {
            if (Game == null || Game.State == null || !Game.InGame) return;
            if (rock)
                rock.showNextHint = rock.Revealed && (Game.State.hintLevel >= 3 || rock.Mistakes >= 3);
            if (IsCurrent(QuestStep.TheaterFindInscription) && !m_ApproachToastShown && RockTarget && rock && !rock.Revealed)
            {
                var d = Game.Rig.Head.position - RockTarget.position; d.y = 0;
                if (d.magnitude < rockApproachDistance)
                {
                    m_ApproachToastShown = true;
                    Game.Menus.ShowToast("toast.rock_approach", 5f);
                }
            }
            if (IsCurrent(QuestStep.TheaterIntro) && !m_IntroStarted && stageCenter)
            {
                var d = Game.Rig.Head.position - stageCenter.position; d.y = 0;
                if (d.magnitude < arriveDistance)
                {
                    m_IntroStarted = true;
                    if (maliku) maliku.Play(NpcGesture.Wave, 3f);
                    Game.Dialogue.Play("theater_intro", () => Complete(QuestStep.TheaterIntro));
                }
            }
        }

        void StartPerformance()
        {
            if (!director) { Game.State.performanceSeen = true; Complete(QuestStep.TheaterPerformance); return; }
            director.Play(() =>
            {
                Game.State.performanceSeen = true;
                Complete(QuestStep.TheaterPerformance); // no-op if the play was replayed after this step
            });
        }

        void SkipPerformance()
        {
            if (director && director.Playing) director.Skip();
        }

        void ReplayPerformance()
        {
            Game.Dialogue.StopAll();
            StartPerformance();
        }

        void OnInscriptionRevealed()
        {
            HegraAudio.PlayAt(Sfx.Carve, inscription.transform.position);
            if (!rock)
            {
                if (Game.GrantSign(SignId.Inscription)) Game.Dialogue.Play("theater_sign");
                return;
            }
            // revealing only uncovers the letters; Sign 1 comes from repeating the glowing sequence
            Game.State.rockGlyphsRevealed = true;
            Game.NotifyStateChanged();
            if (IsCurrent(QuestStep.TheaterFindInscription)) ShowRepeatObjective();
            Game.Dialogue.Play("rock_revealed");
            rock.PlaySequence(1.2f);
        }

        void OnRockMistake()
        {
            Game.Menus.ShowToast(rock.Mistakes >= 2 ? "toast.rock_hint" : "toast.rock_wrong", 4f);
        }

        void OnRockSolved()
        {
            if (Game.GrantSign(SignId.Inscription))
                Game.Dialogue.Play("theater_sign");
        }

        public override bool CanResetPuzzle => IsCurrent(QuestStep.TheaterPerformance) || IsCurrent(QuestStep.TheaterFindInscription);

        public override void ResetPuzzle()
        {
            if (IsCurrent(QuestStep.TheaterFindInscription) && rock && rock.Revealed && !rock.IsSolved)
            {
                rock.PlaySequence(0.5f);
                Game.Menus.ShowToast("toast.puzzle_reset");
                return;
            }
            ReplayPerformance();
        }
    }
}
