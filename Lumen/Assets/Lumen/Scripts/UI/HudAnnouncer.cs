using System;
using Lumen.Art;
using Lumen.Core;
using Lumen.Level;
using UnityEngine;

namespace Lumen.UI
{
    /// <summary>
    /// Turns gameplay events into HUD banners and popups (score, snuffed braziers, pickups,
    /// revives, blackout warning, the Guardian waking). Gameplay never references the HUD:
    /// this listens on <see cref="GameEvents"/>. Banners only show during play, so the
    /// attract-mode demo behind the title stays clean.
    /// </summary>
    public class HudAnnouncer : IDisposable
    {
        readonly Hud hud;
        readonly Func<bool> playing;

        public HudAnnouncer(Hud hud, Func<bool> isPlaying)
        {
            this.hud = hud;
            playing = isPlaying;
            GameEvents.ScoreGained += OnScore;
            GameEvents.BrazierExtinguished += OnBrazierOut;
            GameEvents.ItemCollected += OnItem;
            GameEvents.PlayerRevived += OnRevived;
            GameEvents.BlackoutWarning += OnBlackoutWarning;
            GameEvents.GuardianAwoke += OnGuardianAwoke;
        }

        public void Dispose()
        {
            GameEvents.ScoreGained -= OnScore;
            GameEvents.BrazierExtinguished -= OnBrazierOut;
            GameEvents.ItemCollected -= OnItem;
            GameEvents.PlayerRevived -= OnRevived;
            GameEvents.BlackoutWarning -= OnBlackoutWarning;
            GameEvents.GuardianAwoke -= OnGuardianAwoke;
        }

        /// <summary>The floor event gets its own banner, shown after the floor's title card.</summary>
        public void FloorEvent(FloorEventKind kind)
        {
            string desc = Locale.Get(FloorEvents.DescKey(kind));
            hud.Announce(Locale.Get(FloorEvents.NameKey(kind)), char.ToUpper(desc[0]) + desc.Substring(1), 3f, Palette.UiDanger);
        }

        void OnScore(int amount, Vector2 pos) => hud.ScorePopup(pos, amount);

        void OnBrazierOut(Vector2 _)
        {
            if (playing()) hud.Announce("", Locale.Get("hud.relit"), 2f);
        }

        void OnItem(Vector2 _, ItemKind kind) => hud.Announce("", Locale.Get("item." + kind), 1.6f);

        void OnRevived(Vector2 _) => hud.Announce(Locale.Get("hud.revived"), "", 2f, Palette.FireYellow);

        void OnBlackoutWarning()
        {
            if (playing()) hud.Announce(Locale.Get("event.Blackout.warn"), "", 2f, Palette.UiDanger);
        }

        void OnGuardianAwoke(Vector2 _)
        {
            if (playing()) hud.Announce(Locale.Get("boss.awake"), Locale.Get("boss.awake.sub"), 2.6f, Palette.UiDanger);
        }
    }
}
