using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using MeryanovPayment.Models;

namespace MeryanovPayment.Services
{
    /// <summary>
    /// Экспорт данных в Microsoft Word и Excel (Interop)
    /// Согласно шагам 13–14 методических указаний
    /// </summary>
    public static class ExportService
    {
        /// <summary>
        /// Экспорт в Microsoft Word (.docx + .pdf)
        /// </summary>
        public static void ExportToWord(List<PaymentView> payments, List<Category> categories, List<User> users)
        {
            dynamic wordApp = null;
            dynamic document = null;

            try
            {
                Type wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null)
                {
                    MessageBox.Show("Microsoft Word не установлен на этом компьютере.",
                        "Экспорт в Word", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                wordApp = Activator.CreateInstance(wordType);
                wordApp.Visible = false;
                document = wordApp.Documents.Add();

                // Группируем платежи по пользователям
                var usersWithPayments = payments
                    .GroupBy(p => new { p.UserID, p.UserFIO })
                    .OrderBy(g => g.Key.UserFIO)
                    .ToList();

                bool first = true;
                foreach (var userGroup in usersWithPayments)
                {
                    if (!first)
                        document.Words.Last.InsertBreak(7); // wdPageBreak = 7
                    first = false;

                    // Заголовок — ФИО пользователя
                    dynamic userPara = document.Paragraphs.Add();
                    dynamic userRange = userPara.Range;
                    userRange.Text = userGroup.Key.UserFIO;
                    try { userPara.set_Style("Заголовок 1"); } catch { userRange.Font.Bold = 1; userRange.Font.Size = 16; }
                    userRange.ParagraphFormat.Alignment = 1; // center
                    userRange.InsertParagraphAfter();
                    document.Paragraphs.Add();

                    // Таблица: Категория | Сумма расходов
                    var categoryTotals = categories.Select(c => new
                    {
                        Name = c.Name,
                        Total = userGroup.Where(p => p.CategoryID == c.ID).Sum(p => p.Total)
                    }).Where(x => x.Total > 0).ToList();

                    if (categoryTotals.Count == 0)
                    {
                        dynamic emptyPara = document.Paragraphs.Add();
                        emptyPara.Range.Text = "Платежей нет.";
                        emptyPara.Range.InsertParagraphAfter();
                        continue;
                    }

                    dynamic tablePara = document.Paragraphs.Add();
                    dynamic tableRange = tablePara.Range;
                    dynamic table = document.Tables.Add(tableRange, categoryTotals.Count + 1, 2);
                    table.Borders.InsideLineStyle = 1;
                    table.Borders.OutsideLineStyle = 1;
                    table.Range.Cells.VerticalAlignment = 1; // center

                    // Заголовки
                    table.Cell(1, 1).Range.Text = "Категория";
                    table.Cell(1, 2).Range.Text = "Сумма расходов";
                    table.Rows[1].Range.Font.Name = "Times New Roman";
                    table.Rows[1].Range.Font.Size = 14;
                    table.Rows[1].Range.Bold = 1;
                    table.Rows[1].Range.ParagraphFormat.Alignment = 1;

                    for (int i = 0; i < categoryTotals.Count; i++)
                    {
                        table.Cell(i + 2, 1).Range.Text = categoryTotals[i].Name;
                        table.Cell(i + 2, 1).Range.Font.Name = "Times New Roman";
                        table.Cell(i + 2, 1).Range.Font.Size = 12;

                        table.Cell(i + 2, 2).Range.Text = categoryTotals[i].Total.ToString("N2") + " руб.";
                        table.Cell(i + 2, 2).Range.Font.Name = "Times New Roman";
                        table.Cell(i + 2, 2).Range.Font.Size = 12;
                    }

                    document.Paragraphs.Add();

                    // Максимальный платёж
                    var maxPayment = userGroup.OrderByDescending(p => p.Total).FirstOrDefault();
                    if (maxPayment != null)
                    {
                        dynamic maxPara = document.Paragraphs.Add();
                        dynamic maxRange = maxPara.Range;
                        maxRange.Text = $"Самый дорогостоящий платёж — {maxPayment.Name} за {maxPayment.Total:N2} руб. от {maxPayment.Date:dd.MM.yyyy}";
                        try { maxPara.set_Style("Подзаголовок"); } catch { }
                        maxRange.Font.Color = 128; // dark red-ish
                        maxRange.InsertParagraphAfter();
                    }

                    // Минимальный платёж
                    var minPayment = userGroup.OrderBy(p => p.Total).FirstOrDefault();
                    if (minPayment != null)
                    {
                        dynamic minPara = document.Paragraphs.Add();
                        dynamic minRange = minPara.Range;
                        minRange.Text = $"Самый дешёвый платёж — {minPayment.Name} за {minPayment.Total:N2} руб. от {minPayment.Date:dd.MM.yyyy}";
                        try { minPara.set_Style("Подзаголовок"); } catch { }
                        minRange.Font.Color = 32768; // green
                        minRange.InsertParagraphAfter();
                    }

                    document.Paragraphs.Add();
                }

                // Колонтитулы
                foreach (dynamic section in document.Sections)
                {
                    dynamic footer = section.Footers[1]; // primary
                    try { footer.PageNumbers.Add(1); } catch { } // center

                    dynamic headerRange = section.Headers[1].Range;
                    headerRange.Text = DateTime.Now.ToString("dd.MM.yyyy");
                    headerRange.ParagraphFormat.Alignment = 1;
                    headerRange.Font.Size = 10;
                }

                // Сохранение
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string docxPath = System.IO.Path.Combine(desktop, "Payments_Meryanov.docx");
                string pdfPath = System.IO.Path.Combine(desktop, "Payments_Meryanov.pdf");

                document.SaveAs2(docxPath);
                try { document.SaveAs2(pdfPath, 17); } catch { } // wdExportFormatPDF = 17

                wordApp.Visible = true;

                MessageBox.Show(
                    $"Документ сохранён:\n{docxPath}\n{pdfPath}",
                    "Экспорт в Word", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Word:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Не закрываем Word — пользователь видит документ
            }
        }

        /// <summary>
        /// Экспорт в Microsoft Excel
        /// </summary>
        public static void ExportToExcel(List<PaymentView> payments, List<Category> categories)
        {
            dynamic excelApp = null;
            dynamic workbook = null;

            try
            {
                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null)
                {
                    MessageBox.Show("Microsoft Excel не установлен на этом компьютере.",
                        "Экспорт в Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var users = payments
                    .GroupBy(p => new { p.UserID, p.UserFIO })
                    .OrderBy(g => g.Key.UserFIO)
                    .ToList();

                excelApp = Activator.CreateInstance(excelType);
                excelApp.SheetsInNewWorkbook = Math.Max(users.Count, 1);
                workbook = excelApp.Workbooks.Add();
                excelApp.DisplayAlerts = false;

                decimal grandTotal = 0;

                for (int i = 0; i < users.Count; i++)
                {
                    var userGroup = users[i];
                    dynamic worksheet = workbook.Worksheets[i + 1];
                    string sheetName = userGroup.Key.UserFIO;
                    if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);
                    worksheet.Name = sheetName;

                    int row = 1;

                    // Заголовки
                    worksheet.Cells[1, 1] = "Дата платежа";
                    worksheet.Cells[1, 2] = "Название";
                    worksheet.Cells[1, 3] = "Стоимость";
                    worksheet.Cells[1, 4] = "Количество";
                    worksheet.Cells[1, 5] = "Сумма";

                    dynamic headerRange = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[1, 5]];
                    headerRange.HorizontalAlignment = -4108; // center
                    headerRange.Font.Bold = true;
                    row = 2;

                    // Группировка по категориям
                    var byCategory = userGroup
                        .OrderBy(p => p.Date)
                        .GroupBy(p => p.CategoryName)
                        .OrderBy(g => g.Key);

                    foreach (var catGroup in byCategory)
                    {
                        // Название категории (объединённые ячейки)
                        dynamic catRange = worksheet.Range[worksheet.Cells[row, 1], worksheet.Cells[row, 5]];
                        catRange.Merge();
                        catRange.Value = catGroup.Key;
                        catRange.HorizontalAlignment = -4108;
                        catRange.Font.Italic = true;
                        row++;

                        int catStartRow = row;
                        foreach (var payment in catGroup)
                        {
                            worksheet.Cells[row, 1] = payment.Date.ToString("dd.MM.yyyy");
                            worksheet.Cells[row, 2] = payment.Name;
                            worksheet.Cells[row, 3] = (double)payment.Price;
                            worksheet.Cells[row, 3].NumberFormat = "0.00";
                            worksheet.Cells[row, 4] = payment.Num;
                            worksheet.Cells[row, 5].Formula = $"=C{row}*D{row}";
                            worksheet.Cells[row, 5].NumberFormat = "0.00";
                            row++;
                        }

                        // ИТОГО по категории
                        dynamic sumRange = worksheet.Range[worksheet.Cells[row, 1], worksheet.Cells[row, 4]];
                        sumRange.Merge();
                        sumRange.Value = "ИТОГО:";
                        sumRange.HorizontalAlignment = -4152; // right
                        worksheet.Cells[row, 5].Formula = $"=SUM(E{catStartRow}:E{row - 1})";
                        sumRange.Font.Bold = true;
                        worksheet.Cells[row, 5].Font.Bold = true;
                        worksheet.Cells[row, 5].NumberFormat = "0.00";
                        row++;
                    }

                    // Границы
                    if (row > 2)
                    {
                        dynamic borderRange = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[row - 1, 5]];
                        borderRange.Borders.LineStyle = 1;
                    }

                    worksheet.Columns.AutoFit();
                    grandTotal += userGroup.Sum(p => p.Total);
                }

                // Лист «Общий итог»
                dynamic summarySheet = workbook.Worksheets.Add(After: workbook.Worksheets[workbook.Worksheets.Count]);
                summarySheet.Name = "Общий итог";
                summarySheet.Cells[1, 1] = "Общий итог:";
                summarySheet.Cells[1, 2] = (double)grandTotal;
                summarySheet.Cells[1, 2].NumberFormat = "0.00";
                dynamic summaryRange = summarySheet.Range[summarySheet.Cells[1, 1], summarySheet.Cells[1, 2]];
                summaryRange.Font.Color = 255; // red
                summaryRange.Font.Bold = true;
                summarySheet.Columns.AutoFit();

                excelApp.DisplayAlerts = true;
                excelApp.Visible = true;

                MessageBox.Show("Данные успешно выгружены в Microsoft Excel.",
                    "Экспорт в Excel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Excel:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
