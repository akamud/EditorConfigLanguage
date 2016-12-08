﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace EditorConfig
{
    internal sealed class SeverityAdornmentTagger : IntraTextAdornmentTagger<SeverityTag, SeverityAdornment>
    {
        internal static ITagger<IntraTextAdornmentTag> GetTagger(IWpfTextView view, Lazy<ITagAggregator<SeverityTag>> colorTagger)
        {
            return view.Properties.GetOrCreateSingletonProperty(() => new SeverityAdornmentTagger(view, colorTagger.Value));
        }

        private ITagAggregator<SeverityTag> _alertTagger;

        private SeverityAdornmentTagger(IWpfTextView view, ITagAggregator<SeverityTag> alertTagger)
        : base(view)
        {
            _alertTagger = alertTagger;
        }

        public void Dispose()
        {
            _alertTagger.Dispose();

            view.Properties.RemoveProperty(typeof(SeverityAdornmentTagger));
        }

        // To produce adornments that don't obscure the text, the adornment tags
        // should have zero length spans. Overriding this method allows control
        // over the tag spans.
        protected override IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, SeverityTag>> GetAdornmentData(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            var alertTags = _alertTagger.GetTags(spans);

            foreach (IMappingTagSpan<SeverityTag> dataTagSpan in alertTags)
            {
                var alertTagSpans = dataTagSpan.Span.GetSpans(snapshot);

                // Ignore data tags that are split by projection.
                // This is theoretically possible but unlikely in current scenarios.
                if (alertTagSpans.Count != 1)
                    continue;

                SnapshotSpan adornmentSpan = new SnapshotSpan(alertTagSpans[0].End, 0);

                yield return Tuple.Create(adornmentSpan, (PositionAffinity?)PositionAffinity.Successor, dataTagSpan.Tag);
            }
        }

        protected override SeverityAdornment CreateAdornment(SeverityTag dataTag, SnapshotSpan span)
        {
            return new SeverityAdornment(dataTag);
        }

        protected override bool UpdateAdornment(SeverityAdornment adornment, SeverityTag dataTag)
        {
            adornment.Update(dataTag);
            return true;
        }
    }
}