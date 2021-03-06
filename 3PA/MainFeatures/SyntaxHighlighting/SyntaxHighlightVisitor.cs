﻿#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SyntaxHighlightVisitor.cs) is part of 3P.
// 
// 3P is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// 3P is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with 3P. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Linq;
using _3PA.MainFeatures.AutoCompletionFeature;
using _3PA.MainFeatures.Parser;
using _3PA.MainFeatures.Parser.Pro;
using _3PA.MainFeatures.Parser.Pro.Tokenize;
using _3PA.NppCore;
using Tokenizer = _3PA.MainFeatures.Parser.Tokenizer;

namespace _3PA.MainFeatures.SyntaxHighlighting {

    internal class SyntaxHighlightVisitor : ITokenizerVisitor {

        private int _includeDepth;

        private char[] _operatorChars = { '=', '+', '-', '/', '*', '^', '<', '>' };

        public string[] NormedVariablesPrefixes { get; set; }

        public void PreVisit(Tokenizer lexer) {
            var proLexer = lexer as ProTokenizer;
            if (proLexer != null) {
                _includeDepth = proLexer.IncludeDepth;
                Sci.StartStyling(proLexer.Offset);
            }
        }

        public void Visit(TokenComment tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Comment);
        }

        public void Visit(TokenEol tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.WhiteSpace);
        }

        public void Visit(TokenEos tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Default);
        }

        public void Visit(TokenInclude tok) {
            _includeDepth++;
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Include);
        }

        public void Visit(TokenPreProcVariable tok) {
            _includeDepth++;
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Include);
        }

        public void Visit(TokenNumber tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Number);
        }

        public void Visit(TokenString tok) {
            if (tok.Value != null && tok.Value[0] == '\'') {
                SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.SimpleQuote);
            } else {
                SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.DoubleQuote);
            }
        }

        public void Visit(TokenStringDescriptor tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Default);
        }

        public void Visit(TokenSymbol tok) {
            SciStyleId style = SciStyleId.Default;
            if (_includeDepth > 0 && tok.Value == "}") {
                _includeDepth--;
                style = SciStyleId.Include;
            } else if (tok.EndPosition - tok.StartPosition == 1 && _operatorChars.Contains(tok.Value[0])) {
                style = SciStyleId.Operator;
            }
            SetStyling(tok.EndPosition - tok.StartPosition, style);
        }

        public void Visit(TokenEof tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.WhiteSpace);
        }

        public void Visit(TokenWord tok) {
            SciStyleId style = SciStyleId.Default;
            if (_includeDepth > 0)
                style = SciStyleId.Include;
            else {
                var existingKeywords = Keywords.Instance.GetKeywordsByName(tok.Value);
                if (existingKeywords != null && existingKeywords.Count > 0) {
                    style = existingKeywords.First().KeywordSyntaxStyle;
                }

                // normed variables
                if (style == SciStyleId.Default) {
                    var pos = tok.Value.IndexOf("_", StringComparison.CurrentCultureIgnoreCase);
                    if (pos > 0 && tok.Value.Length >= pos + 1) {
                        var prefix = tok.Value.Substring(0, pos + 1);
                        if (NormedVariablesPrefixes.Contains(prefix))
                            style = SciStyleId.NormedVariables;
                    }
                }
            }
            SetStyling(tok.EndPosition - tok.StartPosition, style);
        }

        public void Visit(TokenWhiteSpace tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.WhiteSpace);
        }

        public void Visit(TokenUnknown tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Default);
        }

        public void Visit(TokenPreProcDirective tok) {
            SetStyling(tok.EndPosition - tok.StartPosition, SciStyleId.Preprocessor);
        }

        public void PostVisit() {
        }

        private void SetStyling(int length, SciStyleId style) {
            Sci.SetStyling(length, (int)style);
        }
    }
}