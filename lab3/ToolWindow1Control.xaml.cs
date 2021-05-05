using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Text;
using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Text.RegularExpressions;

namespace Lab_3
{
    public class Statistic
    {
        public string FunctionName { get; set; }
        public string KeywordCount { get; set; }
        public string LinesCount { get; set; }
        public string WithoutComments { get; set; }
        public string ClassName { get; set; }
        //private int[] getOffsets()
        //{
        //    System.Drawing.Font drawFont = new System.Drawing.Font("Segoe UI", 12);
        //    int[] offsets = { 100,100,100,100,100, 15, 15, 15, 15, 15, 14, 16, 15, 49 };
        //    return offsets;
        //}
        //public void align()
        //{
        //    int[] offsets = getOffsets();
        //    FunctionName = FunctionName.PadLeft(offsets[0]); KeywordCount = KeywordCount.PadLeft(offsets[1]);
        //    LinesCount = LinesCount.PadLeft(offsets[2]); WithoutComments = WithoutComments.PadLeft(offsets[3]);
        //    ClassName = ClassName.PadLeft(offsets[4]);
        //}
    }


    public partial class ToolWindow1Control : UserControl
    {
        private List<Statistic> table;

        public ToolWindow1Control()
        {
            this.InitializeComponent();
            table = new List<Statistic>();

            Statistic.ItemsSource = table;

        }

 
        private void funcInfo(string className, CodeElement codeElement)
        {

            Dispatcher.VerifyAccess();
            CodeFunction funcElement = codeElement as CodeFunction;
            TextPoint start = funcElement.GetStartPoint(vsCMPart.vsCMPartHeader);
            TextPoint finish = funcElement.GetEndPoint();
            string fullSource = start.CreateEditPoint().GetText(finish);
            int openCurlyBracePos = fullSource.IndexOf('{');
            if (openCurlyBracePos > -1)
            {

                string prototype = fullSource.Substring(0, openCurlyBracePos).Trim();
                string pattern = @"\b(alignas|alignof|and|and_eq|asm|auto|bitand|bitor|bool|break|case|catch|char|char16_t|char32_t|class|compl|const|constexpr|const_cast|continue|decltype|default|delete|do|double|dynamic_cast|else|enum|explicit|export|extern|false|float|for|friend|goto|if|inline|int|long|mutable|namespace|new|noexcept|not|not_eq|nullptr|operator|or|or_eq|private|protected|public|register|reinterpret_cast|return|short|signed|sizeof|static|static_assert|static_cast|struct|switch|template|this|thread_local|throw|true|try|typedef|typeid|typename|union|unsigned|using|virtual|void|volatile|wchar_t|while|xor|xor_eq)\b";
                int linesAll = Regex.Matches(fullSource, @"[\n]").Count + 1;
                string uncommented = DeleteCom(fullSource, false);
                string uncommented_with_quot = DeleteCom(fullSource, true);

                int count_of_lines_wt_comments = Regex.Matches(uncommented, @"[\n]").Count+1;
               /// int linesAll = Regex.Matches(uncommented_with_quot, @"[\n]").Count + 1;
                count_of_lines_wt_comments++;
                int comments_count = CommentCount(fullSource);
                int keywords = Regex.Matches(uncommented_with_quot, pattern).Count;
                string str_keyword = keywords.ToString();
                string str_lines = linesAll.ToString();
                string str_lines1 = (linesAll- comments_count).ToString();
                table.Add(new Statistic()
                {
                    FunctionName = prototype,
                    LinesCount = str_lines,
                    WithoutComments = str_lines1,
                    KeywordCount = str_keyword,
                    ClassName=className
                });
                
            }
        }
        private int CommentCount(string code)
        {
            var Line = @"(//(?:.*\\[\n\r]{2}){1,}.*[\n\r])|(//(?:[^""\n])*\n)|(/\*[\s\S]*?\*/)|((?:R""([^(\\\s]{0,16})\([^)]*\)\2"")|(""\\\ \r)|(""(?:\?\?'|\\\\|\\""|\\\n|[^""])*?"")|(?:""[^""]*?"")|(?:'(?:\\\\|\\'|\\\n|[^'])*?'))";
            int matches = 0;

            MatchCollection kek = Regex.Matches(code, Line, RegexOptions.Multiline);
            foreach (Match match in kek)
            {
                if (match.ToString().IndexOf("//", 0, 2) == -1 && match.ToString().IndexOf("/*", 0, 2) == -1) continue;
                if (Regex.Matches(match.ToString(), "\n").Count > 1)
                    matches += Regex.Matches(match.ToString(), "\n").Count - Regex.Matches(match.ToString(), "\n$").Count + 1;
                else
                    matches++;
            }
            return matches;
        }

        private string DeleteCom(string source, bool flag)
        {
            string JustComment = @"/\*((.*?)|(((.*?)\n)+(.*?)))\*/";
            string SlashComment = @"//((\r\n)|((.*?)[^\\]\r\n))";
            string QuotationQuote = @"""(("")|(\r\n)|((.*?)(([^\\]\r\n)|([^\\]""))))";
            string ApostropheQuote = @"'((')|(\r\n)|((.*?)(([^\\]\r\n)|([^\\]'))))";
            string EmptyLine = @"[\n\r]\s*[\r\n]";

            var CommentQuoteRegex =
                JustComment + "|" +
                SlashComment + "|" +
                QuotationQuote + "|" +
                ApostropheQuote;

            source = Regex.Replace(source,
                CommentQuoteRegex,
                match =>
                {
                    if (match.Value.StartsWith("//") ||
                        match.Value.StartsWith("/*"))
                    {
                        return Environment.NewLine;
                    }

                    if (flag && (
                        match.Value.StartsWith("\"") ||
                        match.Value.StartsWith("\'")))
                    {
                        return Environment.NewLine;
                    }

                    return match.Value;
                }, RegexOptions.Singleline);
            var EmptyLineRegex = new Regex(EmptyLine);
            var SecondClean = EmptyLineRegex.Replace(source, "\n");
            return SecondClean;
        }

        private void Parse(FileCodeModel2 model)
        {
            Dispatcher.VerifyAccess();
            foreach (CodeElement codeElement in model.CodeElements)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
                {
                    CodeNamespace namespaceElement = codeElement as CodeNamespace;
                    foreach (CodeElement elem in namespaceElement.Children)
                    {
                        if (elem.Kind == vsCMElement.vsCMElementFunction)
                        {
                            CodeFunction funcElement = elem as CodeFunction;
                            funcInfo("",(CodeElement)funcElement);
                        }
                    }
                }
                if (codeElement.Kind == vsCMElement.vsCMElementClass)
                {
                    CodeClass classElement = codeElement as CodeClass;
                    foreach (CodeElement elem in classElement.Children)
                    {
                        if (elem.Kind == vsCMElement.vsCMElementFunction)
                        {
                            CodeFunction funcElement = elem as CodeFunction;
                            funcInfo(classElement.FullName, elem);
                        }
                    }

                }
                if (codeElement.Kind == vsCMElement.vsCMElementFunction)
                {
                    funcInfo("",codeElement);
                }
            }
        }

        private void MenuItemCallback()
        {
            Dispatcher.VerifyAccess();
            table.Clear();
            DTE2 dte;
            try
            {
                dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
                ProjectItem item = dte.ActiveDocument.ProjectItem;
                FileCodeModel2 model = (FileCodeModel2)item.FileCodeModel;
                if (model != null)
                    Parse(model);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MenuItemCallback();
            Statistic.Items.Refresh();
        }
        private void StatisticsListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumnsWidth(sender as ListView);

        }

        private void StatisticsListView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateColumnsWidth(sender as ListView);
        }

        private void UpdateColumnsWidth(ListView listView)
        {
            var columnWidth = listView.ActualWidth / (listView.View as GridView).Columns.Count;

            foreach (var column in (listView.View as GridView).Columns)
            {
                column.Width = columnWidth;
            }
        }

    }
}