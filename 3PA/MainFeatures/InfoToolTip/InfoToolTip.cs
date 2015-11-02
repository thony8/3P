﻿#region Header
// // ========================================================================
// // Copyright (c) 2015 - Julien Caillon (julien.caillon@gmail.com)
// // This file (InfoToolTip.cs) is part of 3P.
// 
// // 3P is a free software: you can redistribute it and/or modify
// // it under the terms of the GNU General Public License as published by
// // the Free Software Foundation, either version 3 of the License, or
// // (at your option) any later version.
// 
// // 3P is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// // GNU General Public License for more details.
// 
// // You should have received a copy of the GNU General Public License
// // along with 3P. If not, see <http://www.gnu.org/licenses/>.
// // ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using YamuiFramework.HtmlRenderer.Core.Core.Entities;
using _3PA.Lib;
using _3PA.MainFeatures.AutoCompletion;
using _3PA.MainFeatures.Parser;

namespace _3PA.MainFeatures.InfoToolTip {
    class InfoToolTip {

        #region fields
        // The tooltip form
        private static InfoToolTipForm _form;

        // we save the conditions with which we showed the tooltip to be able to update it as is
        private static List<CompletionData> _currentCompletionList;
        private static Point _currentPosition;
        private static int _currentLineHeight;

        /// <summary>
        /// Was the form opened because the user left his mouse too long on a word?
        /// </summary>
        private static bool _openedFromDwell;

        /// <summary>
        /// If a tooltip is opened and it's a parsed item, this point leads to its definition
        /// </summary>
        public static Point GoToDefinitionPoint = new Point(-1, -1);

        /// <summary>
        /// Index of the tooltip to show in case where a word corresponds to several items in the
        /// CompletionData list
        /// </summary>
        public static int IndexToShow;
        #endregion

        #region Tooltip
        /// <summary>
        /// Method called when the tooltip is opened from the mouse being inactive on scintilla
        /// </summary>
        public static void ShowToolTipFromDwell(bool openTemporary = true) {
            _openedFromDwell = openTemporary;
            if (Config.Instance.ToolTipDeactivate) return;

            InitIfneeded();

            var position = Npp.GetPositionFromMouseLocation();

            // sets the tooltip content
            if (position < 0) 
                return;
            var data = AutoComplete.FindInCompletionData(Npp.GetWordAtPosition(position), position);
            if (data != null && data.Count == 0) return;

            // save the current list
            _currentCompletionList = data;

            SetToolTip(data);

            // update position
            var point = Npp.GetPointXyFromPosition(position);
            point.Offset(Npp.GetWindowRect().Location);
            var lineHeight = Npp.GetTextHeight(Npp.GetCaretLineNumber());
            point.Y += lineHeight + 5;
            _form.SetPosition(point, lineHeight + 5);

            // save current
            _currentPosition = point;
            _currentLineHeight = lineHeight;

            if (!_form.Visible)
                _form.UnCloack();
        }

        /// <summary>
        /// Called when a tooltip is shown and the user presses CTRL + down/up to show 
        /// the other definitions available
        /// </summary>
        public static void TryToShowIndex() {
            if (_currentCompletionList == null) return;

            // refresh tooltip with the correct index
            _form.Cloack();
            SetToolTip(_currentCompletionList);
            _form.SetPosition(_currentPosition, _currentLineHeight + 5);
            if (!_form.Visible)
                _form.UnCloack();
        }

        /// <summary>
        /// Method called when the tooltip is opened to help the user during autocompletion
        /// </summary>
        public static void ShowToolTipFromAutocomplete() {
            if (Config.Instance.ToolTipDeactivate) return;

            InitIfneeded();

            var position = Npp.GetPositionFromMouseLocation();

            // sets the tooltip content
            if (position < 0)
                return;
            var data = AutoComplete.FindInCompletionData(Npp.GetWordAtPosition(position), position);
            if (data != null && data.Count == 0) return;
            SetToolTip(data);

            // update position
            var point = Npp.GetPointXyFromPosition(position);
            point.Offset(Npp.GetWindowRect().Location);
            var lineHeight = Npp.GetTextHeight(Npp.GetCaretLineNumber());
            point.Y += lineHeight + 5;
            _form.SetPosition(point, lineHeight + 5);

            if (!_form.Visible)
                _form.UnCloack();
        }

        /// <summary>
        /// Handles the clicks on the link displayed in the tooltip
        /// </summary>
        /// <param name="htmlLinkClickedEventArgs"></param>
        private static void ClickHandler(HtmlLinkClickedEventArgs htmlLinkClickedEventArgs) {
            UserCommunication.Notify(htmlLinkClickedEventArgs.Link);
        }
        #endregion

        #region SetToolTip text

        /// <summary>
        /// Sets the content of the tooltip (when we want to descibe something present
        /// in the completionData list)
        /// </summary>
        private static void SetToolTip(List<CompletionData> listOfCompletionData) {
            
            var toDisplay = new StringBuilder();

            // only select one item from the list
            if (IndexToShow < 0) IndexToShow = listOfCompletionData.Count - 1;
            if (IndexToShow >= listOfCompletionData.Count) IndexToShow = 0;
            var data = listOfCompletionData.ElementAt(IndexToShow);

            // general stuff
            toDisplay.Append("<div class='InfoToolTip'>");
            toDisplay.Append(@"
                <table width='100%' class='ToolTipName'><tr style='vertical-align: top;'>
                <td>
                    <img style='padding-right: 7px;' src ='" + data.Type + "'>" + data.Type + @"
                </td>");
            if (listOfCompletionData.Count > 1)
                toDisplay.Append(@"
                    <td class='ToolTipCount'>" +
                        (IndexToShow + 1) + "/" + listOfCompletionData.Count + @"
                    </td>");
            toDisplay.Append(@"
                </tr></table>");

            // the rest depends on the data type
            try {
                switch (data.Type) {
                    case CompletionType.TempTable:
                    case CompletionType.Table:
                        // buffer
                        if (data.ParsedItem is ParsedDefine)
                            toDisplay.Append(FormatRowWithImg(ParseFlag.Buffer.ToString(), "BUFFER FOR " + FormatSubString(data.SubString)));

                        var tbItem = ParserHandler.FindAnyTableOrBufferByName(data.DisplayText);
                        if (tbItem != null) {
                            if (!string.IsNullOrEmpty(tbItem.Description))
                                toDisplay.Append(FormatRow("Description", tbItem.Description));
                            toDisplay.Append(FormatRow("Number of fields", tbItem.Fields.Count.ToString()));

                            if (tbItem.Triggers.Count > 0) {
                                toDisplay.Append(FormatSubtitle("TRIGGERS"));
                                foreach (var parsedTrigger in tbItem.Triggers)
                                    toDisplay.Append(FormatRow(parsedTrigger.Event, "<a class='ToolGotoDefinition' href='triggerproc#" + parsedTrigger.ProcName + "'>" + parsedTrigger.ProcName + "</a>"));
                            }

                            if (tbItem.Indexes.Count > 0) {
                                toDisplay.Append(FormatSubtitle("INDEXES"));
                                foreach (var parsedIndex in tbItem.Indexes)
                                    toDisplay.Append(FormatRow(parsedIndex.Name, parsedIndex.Flag + " - " + parsedIndex.FieldsList.Aggregate((i, j) => i + ", " + j)));
                            }
                        }
                        break;
                    case CompletionType.Database:
                        var dbItem = DataBase.GetDb(data.DisplayText);

                        toDisplay.Append(FormatRow("Logical name", dbItem.LogicalName));
                        toDisplay.Append(FormatRow("Physical name", dbItem.PhysicalName));
                        toDisplay.Append(FormatRow("Progress version", dbItem.ProgressVersion));
                        toDisplay.Append(FormatRow("Number of Tables", dbItem.Tables.Count.ToString()));
                        break;
                    case CompletionType.Field:
                    case CompletionType.FieldPk:
                        // find field
                        var fieldFound = DataBase.FindFieldByName(data.DisplayText, (ParsedTable) data.ParsedItem);
                        if (fieldFound != null) {
                            if (fieldFound.AsLike == ParsedAsLike.Like) {
                                toDisplay.Append(FormatRow("Is LIKE", fieldFound.TempType));
                            }
                            toDisplay.Append(FormatRow("Type", FormatSubString(data.SubString)));
                            toDisplay.Append(FormatRow("Owner table", ((ParsedTable)data.ParsedItem).Name));
                            if (!string.IsNullOrEmpty(fieldFound.Description))
                                toDisplay.Append(FormatRow("Description", fieldFound.Description));
                            if (!string.IsNullOrEmpty(fieldFound.Format))
                                toDisplay.Append(FormatRow("Format", fieldFound.Format));
                            if (!string.IsNullOrEmpty(fieldFound.InitialValue))
                                toDisplay.Append(FormatRow("Initial value", fieldFound.InitialValue));
                            toDisplay.Append(FormatRow("Order", fieldFound.Order.ToString()));
                        }
  
                        break;
                    case CompletionType.Function:
                        var funcItem = (ParsedFunction) data.ParsedItem;
                        toDisplay.Append(FormatRow("Return type", FormatSubString(funcItem.ParsedReturnType)));
                        if (funcItem.PrototypeLine > 0)
                            toDisplay.Append("<a href=''>Go to prototype</a>");

                        toDisplay.Append(FormatSubtitle("PARAMETERS"));
                        if (!string.IsNullOrEmpty(funcItem.Parameters)) {
                            foreach (var param in funcItem.Parameters.Split(',')) {
                                toDisplay.Append(FormatRowWithImg(ParseFlag.Parameter.ToString(), param.Trim()));
                            }
                        } else
                            toDisplay.Append("No parameters!<br>");
                        break;
                    case CompletionType.Keyword:
                    case CompletionType.KeywordObject:
                        toDisplay.Append(FormatRow("Type of keyword", FormatSubString(data.SubString)));
                        toDisplay.Append(FormatSubtitle("DESCRIPTION"));
                        // TODO
                        toDisplay.Append(FormatSubtitle("SYNTHAX"));
                        // TODO
                        break;
                    case CompletionType.Label:
                        break;
                    case CompletionType.Preprocessed:
                        var preprocItem = (ParsedPreProc) data.ParsedItem;
                        if (preprocItem.UndefinedLine > 0)
                            toDisplay.Append(FormatRow("Undefined line", preprocItem.UndefinedLine.ToString()));
                        break;
                    case CompletionType.Snippet:
                        // TODO
                        break;
                    case CompletionType.VariableComplex:
                    case CompletionType.VariablePrimitive:
                    case CompletionType.Widget:
                        var varItem = (ParsedDefine) data.ParsedItem;
                        toDisplay.Append(FormatRow("Define type", FormatSubString(varItem.Type.ToString())));
                        if (!string.IsNullOrEmpty(varItem.TempPrimitiveType))
                            toDisplay.Append(FormatRow("Variable type", FormatSubString(varItem.PrimitiveType.ToString())));
                        if (varItem.AsLike == ParsedAsLike.Like)
                            toDisplay.Append(FormatRow("Is LIKE", varItem.TempPrimitiveType));
                        if (!string.IsNullOrEmpty(varItem.ViewAs))
                            toDisplay.Append(FormatRow("Screen representation", varItem.ViewAs));
                        if (!string.IsNullOrEmpty(varItem.LcFlagString))
                            toDisplay.Append(FormatRow("Define flags", varItem.LcFlagString));
                        if (!string.IsNullOrEmpty(varItem.Left))
                            toDisplay.Append(FormatRow("Rest of decla", varItem.Left));
                        break;

                }
            } catch (Exception e) {
                toDisplay.Append("Error when appending info :<br>" + e + "<br>");
            }

            // parsed item?
            if (data.FromParser) {
                toDisplay.Append(FormatSubtitle("ORIGINS"));
                toDisplay.Append(FormatRow("Scope name", data.ParsedItem.OwnerName));
                if (!Npp.GetCurrentFilePath().Equals(data.ParsedItem.FilePath))
                    toDisplay.Append(FormatRow("Owner file", data.ParsedItem.FilePath));
            }

            // Flags
            var flagStrBuilder = new StringBuilder();
            foreach (var name in Enum.GetNames(typeof(ParseFlag))) {
                ParseFlag flag = (ParseFlag)Enum.Parse(typeof(ParseFlag), name);
                if (flag == 0) continue;
                if (!data.Flag.HasFlag(flag)) continue;
                flagStrBuilder.Append(FormatRowWithImg(name, "<b>" + name + "</b>"));
            }
            if (flagStrBuilder.Length > 0) {
                toDisplay.Append(FormatSubtitle("FLAGS"));
                toDisplay.Append(flagStrBuilder);
            }

            // parsed item?
            if (data.FromParser) {
                toDisplay.Append(@"<div class='ToolTipBottomGoTo'>
                    [HOLD CTRL] Prevent auto-close<br>
                    [CTRL + B] <a class='ToolGotoDefinition' href='nexttooltip'>Go to definition</a>");
                if (listOfCompletionData.Count > 1)
                    toDisplay.Append("<br>[CTRL + <span class='ToolTipDownArrow'>" + (char)242 + "</span>] <a class='ToolGotoDefinition' href='nexttooltip'>Read next tooltip</a>");
                toDisplay.Append("</div>");
                GoToDefinitionPoint = new Point(data.ParsedItem.Line, data.ParsedItem.Column);
            }

            toDisplay.Append("</div>");
            _form.SetText(toDisplay.ToString());

        }

        #region formatting functions

        private static string FormatRow(string describe, string result) {
            return "- " + describe + " : <b>" + result + "</b><br>";
        }

        private static string FormatRowWithImg(string image, string text) {
            return "<div class='ToolTipRowWithImg'><img style='padding-right: 2px; padding-left: 5px;' src ='" + image + "' height='15px'>" + text + "</div>";
        }

        private static string FormatSubtitle(string text) {
            return "<div class='ToolTipSubTitle'>" + text + "</div>";
        }

        private static string FormatSubString(string text) {
            return "<span class='ToolTipSubString'>" + text.ToUpper() + "</span>";
        }

        #endregion


        #endregion


        #region handle form
        /// <summary>
        /// Method to init the tooltip form if needed
        /// </summary>
        public static void InitIfneeded() {
            // instanciate the form
            if (_form == null) {
                _form = new InfoToolTipForm {
                    UnfocusedOpacity = Config.Instance.ToolTipUnfocusedOpacity,
                    FocusedOpacity = Config.Instance.ToolTipFocusedOpacity
                };
                _form.Show(Npp.Win32WindowNpp);
                _form.SetLinkClickedEvent(ClickHandler);
            }
        }

        /// <summary>
        /// Closes the form
        /// </summary>
        public static void Close(bool calledFromDwellEnd = false) {
            try {
                if (calledFromDwellEnd && !_openedFromDwell) return;
                _form.Cloack();
                _openedFromDwell = false;
                GoToDefinitionPoint = new Point(-1, -1);
            } catch (Exception) {
                // ignored
            }
        }

        /// <summary>
        /// Forces the form to close, only when leaving npp
        /// </summary>
        public static void ForceClose() {
            try {
                _form.ForceClose();
                _form = null;
            } catch (Exception) {
                // ignored
            }
        }

        /// <summary>
        /// Is a tooltip visible?
        /// </summary>
        /// <returns></returns>
        public static bool IsVisible {
            get { return _form != null && _form.Visible; }
        }

        #endregion

    }
}
