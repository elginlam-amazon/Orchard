using System.Collections.Generic;
using Lucene.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lucene.ViewModels
{
    public class LuceneSettingsPartEditViewModel
    {
        public LuceneAnalyzerSelectorMapping[] LuceneAnalyzerSelectorMappings { get; set; }

        public IEnumerable<SelectListItem> LuceneAnalyzerSelectors { get; set; }
    }
}