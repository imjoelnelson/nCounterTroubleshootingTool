using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TS_General_QCmodule
{
    public static class Html
    {
        public class Table : HtmlBase, IDisposable
        {
            public Table(StringBuilder sb, string classAttributes = "", string id = "", int indent = 0) : base(sb)
            {
                if(indent > 0)
                {
                    Append($"<table style=\"margin-left:{indent}px\"");
                    AddOptionalAttributes(classAttributes, id);
                }
                else
                {
                    Append("<table");
                    AddOptionalAttributes(classAttributes, id);
                }
            }

            public void StartHead(string classAttributes = "", string id = "")
            {
                Append("<thead");
                AddOptionalAttributes(classAttributes, id);
            }

            public void Border(bool border)
            {
                Append("<style>");
                if(border)
                {
                    Append("table, th, td {border: 1px solid black;}");
                }
                Append("</style>");
            }

            public void AddCaption(string captionText)
            {
                Append($"<caption>{captionText}</caption>");
            }

            public void EndHead()
            {
                Append("</thead>");
            }

            public void StartFoot(string classAttributes = "", string id = "")
            {
                Append("<tfoot");
                AddOptionalAttributes(classAttributes, id);
            }

            public void EndFoot()
            {
                Append("</tfoot>");
            }

            public void StartBody(string classAttributes = "", string id = "")
            {
                Append("<tbody");
                AddOptionalAttributes(classAttributes, id);
            }

            public void EndBody()
            {
                Append("</tbody>");
            }

            public void Legend(List<string> legend)
            {
                Append("</table>");
                if(legend != null)
                {
                    foreach (string s in legend)
                    {
                        Append($"<p>{s}</p>");
                    }
                }
            }

            public void Dispose()
            {
                Append("</body>");
            }

            public Row AddRow(string classAttributes = "", string id = "")
            {
                return new Row(GetBuilder(), classAttributes, id);
            }
        }

        public class Row : HtmlBase, IDisposable
        {
            public Row(StringBuilder sb, string classAttributes = "", string id = "") : base(sb)
            {
                Append("<tr");
                AddOptionalAttributes(classAttributes, id);
            }
            public void Dispose()
            {
                Append("</tr>");
            }
            public void AddCell(string innerText, string classAttributes = "", string id = "", string colSpan = "", string bkgColor = "")
            {
                Append("<td");
                AddOptionalAttributes(classAttributes, id, colSpan, bkgColor);
                Append(innerText);
                Append("</td>");
            }
            public void AddHeadCell(string innerText, string classAttributes = "", string id = "", string colSpan = "", string bkgColor = "")
            {
                Append("<th");
                AddOptionalAttributes(classAttributes, id, colSpan, bkgColor);
                Append(innerText);
                Append("</th>");
            }
        }

        public abstract class HtmlBase
        {
            private StringBuilder _sb;

            protected HtmlBase(StringBuilder sb)
            {
                _sb = sb;
            }

            public StringBuilder GetBuilder()
            {
                return _sb;
            }

            protected void Append(string toAppend)
            {
                _sb.Append(toAppend);
            }

            protected void AddOptionalAttributes(string className = "", string id = "", string colSpan = "", string bgcolor = "")
            {

                if (!string.IsNullOrEmpty(id))
                {
                    _sb.Append($" id=\"{id}\"");
                }
                if (!string.IsNullOrEmpty(className))
                {
                    _sb.Append($" class=\"{className}\"");
                }
                if (!string.IsNullOrEmpty(colSpan))
                {
                    _sb.Append($" colspan=\"{colSpan}\"");
                }
                if(!string.IsNullOrEmpty(bgcolor))
                {
                    _sb.Append($" bgcolor=\"{bgcolor}\"");
                }
                _sb.Append(">");
            }
        }
    }
}
