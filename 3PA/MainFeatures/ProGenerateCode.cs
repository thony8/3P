﻿#region header
// ========================================================================
// Copyright (c) 2016 - Julien Caillon (julien.caillon@gmail.com)
// This file (ProGenerateCode.cs) is part of 3P.
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
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using YamuiFramework.Forms;
using _3PA.Interop;
using _3PA.MainFeatures.AutoCompletion;
using _3PA.MainFeatures.Parser;

namespace _3PA.MainFeatures {
    internal class ProGenerateCode {

        public static void InsertNew(ProInsertNewType type) {

            switch (type) {

                case ProInsertNewType.Function:
                    object newFunc = new ProNewFunction();
                    if (UserCommunication.Input("Insert function", "Define a new function", ref newFunc) != DialogResult.OK)
                        return;

                    break;

                case ProInsertNewType.Procedure:
                    var eol = Npp.GetEolString;
                    var appbuilderBefore = @"&ANALYZE-SUSPEND _UIB-CODE-BLOCK _PROCEDURE {&name} Procedure" + eol;
                    var appbuilderAfter = @"&ANALYZE-RESUME" + eol;

                    object newProc = new ProNewProcedure();
                    if (UserCommunication.Input("Insert procedure", "Define a new internal procedure", ref newProc) != DialogResult.OK)
                        return;

                    var proNew = newProc as IProNew;
                    if (proNew == null)
                        return;

                    // at caret position
                    RepositionCaretForInsertion(proNew);
                    Npp.ModifyTextAroundCaret(0, 0, "new function" + Npp.GetEolString);

                    break;
            }
        }

        private static void RepositionCaretForInsertion(IProNew proNew) {
            // at caret position
            if (proNew.InsertPosition == ProInsertPosition.CaretPosition) {
                Npp.SetSelection(Npp.GetPosFromLineColumn(Npp.Line.CurrentLine, 0));
            } else {
                var findExisting = ParserHandler.GetParsedItemsList.FirstOrDefault(data => data.Type == CompletionType.Procedure);

                // is there already a proc existing?
                if (findExisting != null) {
                    // try to find a proc block, otherwise do from the proc itself
                    UserCommunication.Notify("existing proc");
                    Npp.SetSelection(Npp.CurrentPosition);

                } else {
                    Npp.TargetWholeDocument();
                    var previousFlags = Npp.SearchFlags;
                    Npp.SearchFlags = SearchFlags.Regex;
                    var foundPos = Npp.SearchInTarget(@"/\*\s+[\*]+\s+Internal Procedures");
                    Npp.SearchFlags = previousFlags;

                    // we found a comment indicating where the proc should be inserted?
                    if (foundPos > -1) {
                        Npp.SetSelection(Npp.GetPosFromLineColumn(Npp.LineFromPosition(foundPos), 0));

                    } else {
                        // we find the ideal pos considering the blocks
                        //var findBlock = ParserHandler.GetParsedItemsList.FirstOrDefault(data => data.Type == )

                        Npp.SetSelection(Npp.CurrentPosition);
                    }
                }
            }
        }

        internal interface IProNew {
            string Name { get; set; }
            ProInsertPosition InsertPosition { get; set; }
        }

        internal class ProNewProcedure : IProNew {
            [YamuiInputDialogItem("Name", Order = 0)]
            public string Name { get; set; }
            [YamuiInputDialogItem("Private procedure", Order = 1)]
            public bool IsPrivate { get; set; }
            [YamuiInputDialogItem("Insertion position", Order = 2)]
            public ProInsertPosition InsertPosition { get; set; }
        }

        internal class ProNewFunction : IProNew {
            [YamuiInputDialogItem("Name", Order = 0)]
            public string Name { get; set; }
            [YamuiInputDialogItem("Return type", Order = 1)]
            public ProFunctionType Type { get; set; }
            [YamuiInputDialogItem("Private function", Order = 2)]
            public bool IsPrivate { get; set; }
            [YamuiInputDialogItem("Insertion position", Order = 3)]
            public ProInsertPosition InsertPosition { get; set; }
        }

        internal enum ProInsertNewType {
            Procedure,
            Function
        }

        internal enum ProInsertPosition {
            [Description("Alphabetical order")]
            AlphabeticalOrder,
            [Description("First")]
            First,
            [Description("Last")]
            Last,
            [Description("At caret position")]
            CaretPosition
        }

        internal enum ProFunctionType {
            [Description("CHARACTER")]
            Character,
            [Description("HANDLE")]
            Handle,
            [Description("INTEGER")]
            Integer,
            [Description("LOGICAL")]
            Logical,
            [Description("COM-HANDLE")]
            ComHandle,
            [Description("DECIMAL")]
            Decimal,
            [Description("DATE")]
            Date,
            [Description("DATETIME")]
            Datetime,
            [Description("DATETIME-TZ")]
            DatetimeTz,
            [Description("INT64")]
            Int64,
            [Description("LONGCHAR")]
            Longchar,
            [Description("MEMPTR")]
            Memptr,
            [Description("RAW")]
            Raw,
            [Description("RECID")]
            Recid,
            [Description("ROWID")]
            Rowid,
            [Description("WIDGET-HANDLE")]
            WidgetHandle,
            [Description("CLASS XXX")]
            Class
        }

    }
}
