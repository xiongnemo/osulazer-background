﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK;

namespace osu.Game.Rulesets.Catch.UI
{
    public class CatchPlayfield : ScrollingPlayfield
    {
        private readonly BeatmapDifficulty difficulty;

        /// <summary>
        /// The width of the playfield.
        /// The horizontal movement of the catcher is confined in the area of this width.
        /// </summary>
        public const float WIDTH = 512;

        /// <summary>
        /// The center position of the playfield.
        /// </summary>
        public const float CENTER_X = WIDTH / 2;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
            // only check the X position; handle all vertical space.
            base.ReceivePositionalInputAt(new Vector2(screenSpacePos.X, ScreenSpaceDrawQuad.Centre.Y));

        internal Catcher Catcher { get; private set; }

        internal CatcherArea CatcherArea { get; private set; }

        public CatchPlayfield(BeatmapDifficulty difficulty)
        {
            this.difficulty = difficulty;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var trailContainer = new Container
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.TopLeft
            };
            var droppedObjectContainer = new DroppedObjectContainer();

            Catcher = new Catcher(trailContainer, droppedObjectContainer, difficulty)
            {
                X = CENTER_X
            };

            AddRangeInternal(new[]
            {
                droppedObjectContainer,
                Catcher.CreateProxiedContent(),
                HitObjectContainer.CreateProxy(),
                // This ordering (`CatcherArea` before `HitObjectContainer`) is important to
                // make sure the up-to-date catcher position is used for the catcher catching logic of hit objects.
                CatcherArea = new CatcherArea
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.TopLeft,
                    MovableCatcher = Catcher,
                },
                trailContainer,
                HitObjectContainer,
            });

            RegisterPool<Droplet, DrawableDroplet>(50);
            RegisterPool<TinyDroplet, DrawableTinyDroplet>(50);
            RegisterPool<Fruit, DrawableFruit>(100);
            RegisterPool<Banana, DrawableBanana>(100);
            RegisterPool<JuiceStream, DrawableJuiceStream>(10);
            RegisterPool<BananaShower, DrawableBananaShower>(2);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // these subscriptions need to be done post constructor to ensure externally bound components have a chance to populate required fields (ScoreProcessor / ComboAtJudgement in this case).
            NewResult += onNewResult;
            RevertResult += onRevertResult;
        }

        protected override void OnNewDrawableHitObject(DrawableHitObject d)
        {
            ((DrawableCatchHitObject)d).CheckPosition = checkIfWeCanCatch;
        }

        private bool checkIfWeCanCatch(CatchHitObject obj) => Catcher.CanCatch(obj);

        private void onNewResult(DrawableHitObject judgedObject, JudgementResult result)
            => CatcherArea.OnNewResult((DrawableCatchHitObject)judgedObject, result);

        private void onRevertResult(DrawableHitObject judgedObject, JudgementResult result)
            => CatcherArea.OnRevertResult((DrawableCatchHitObject)judgedObject, result);
    }
}
