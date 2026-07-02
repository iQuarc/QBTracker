using ClosedXML.Excel;
using QBTracker.Model;

namespace QBTracker.Invoice
{
   public class InvoiceData
   {
      public string   SellerName      { get; set; } = string.Empty;
      public string   SellerAddress1  { get; set; } = string.Empty;
      public string   SellerAddress2  { get; set; } = string.Empty;
      public string   SellerPhone     { get; set; } = string.Empty;
      public string   SellerEmail     { get; set; } = string.Empty;
      public string   ClientLegalName { get; set; } = string.Empty;
      public string   ClientAddress1  { get; set; } = string.Empty;
      public string   ClientAddress2  { get; set; } = string.Empty;
      public string   ClientShortName { get; set; } = string.Empty;
      public string   InvoiceNumber   { get; set; } = string.Empty;
      public DateTime InvoiceDate     { get; set; } = DateTime.Today;
      public string   Note            { get; set; } = string.Empty;
      public string   Currency        { get; set; } = "$";
      public GroupingType GroupingType { get; set; }
      public RoundingInterval RoundingInterval { get; set; }
      public RoundingType RoundingType { get; set; }
      public IReadOnlyCollection<ProjectData> Projects { get; set; } = [];
   }

   public class ProjectData
   {
      public string ProjectName { get; set; } = string.Empty;
      public DateTime PeriodFrom { get; set; }
      public DateTime PeriodTo { get; set; }
      public IReadOnlyCollection<InvoiceLineItem> LineItems { get; set; } = [];
   }

   public class InvoiceLineItem
   {
      public string  Description { get; set; } = string.Empty;
      public string  GroupingKey { get; set; } = string.Empty;
      public decimal Hours       { get; set; }
      public decimal HourlyRate  { get; set; }
      public decimal Amount      => Hours * HourlyRate;
   }

   public static class InvoiceWorkbookGenerator
   {
      public static void CreateInvoice(InvoiceData data, string filePath)
      {
         using var workbook  = new XLWorkbook();
         var       worksheet = workbook.Worksheets.Add("Invoice");
         worksheet.SheetView.Worksheet.ShowGridLines = false;

         worksheet.Column(1).Width = 3.0;
         worksheet.Column(2).Width = 32;
         worksheet.Column(3).Width = 19;
         worksheet.Column(4).Width = 9;
         worksheet.Column(5).Width = 16;
         worksheet.Column(6).Width = 18;
         worksheet.Column(7).Width = 9.5;
         worksheet.Column(8).Width = 19;
         worksheet.Column(9).Width = 17;
         for (var i = 10; i <= 23; i++)
            worksheet.Column(i).Width = 8.5;

         var row = 1;

         var companyRange = worksheet.Range(row, 2, row, 9);
         companyRange.Merge();
         worksheet.Row(row).Height               = 60;
         companyRange.Style.Font.FontSize        = 25;
         companyRange.Style.Font.Bold            = true;
         companyRange.Style.Font.FontColor       = XLColor.White;
         companyRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#21798F");
         companyRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
         companyRange.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
         worksheet.Cell(row, 2).Value            = data.SellerName;
         row++;

         worksheet.Row(row).Height               = 5;
         companyRange                            = worksheet.Range(row, 2, row, 9);
         companyRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#A6DCEA");
         row++;

         worksheet.Range(row, 2, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#79CBDF");
         row++;
         worksheet.Range(row, 2, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#79CBDF");
         worksheet.Cell(row, 2).Value                               = data.SellerAddress1;
         worksheet.Cell(row, 5).Value                               = "Phone:";
         worksheet.Cell(row, 6).Value                               = data.SellerPhone;
         row++;

         worksheet.Range(row, 2, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#79CBDF");
         worksheet.Cell(row, 2).Value                               = data.SellerAddress2;
         worksheet.Cell(row, 5).Value                               = "Email:";
         worksheet.Cell(row, 6).Value                               = data.SellerEmail;
         row++;

         worksheet.Range(row, 2, row, 9).Style.Fill.BackgroundColor =  XLColor.FromHtml("#79CBDF");
         row                                                        += 2;

         worksheet.Cell(row, 2).Style.Font.Bold      = true;
         worksheet.Cell(row, 2).Style.Font.FontColor = XLColor.FromHtml("#16515F");
         worksheet.Cell(row, 2).Value                = "Bill To:";

         var billToValueRange = worksheet.Range(row, 3, row, 6);
         billToValueRange.Merge();
         billToValueRange.Value                = data.ClientLegalName;
         billToValueRange.Style.Font.FontColor = XLColor.FromHtml("#0F5666");

         worksheet.Cell(row, 8).Style.Font.Bold      = true;
         worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#16515F");
         worksheet.Cell(row, 8).Value                = "Invoice #:";
         worksheet.Cell(row, 9).Value                = data.InvoiceNumber;
         row++;

         worksheet.Cell(row, 2).Value = "Address:";
         var address1Range = worksheet.Range(row, 3, row, 6);
         address1Range.Merge();
         address1Range.Value                = data.ClientAddress1;
         address1Range.Style.Font.FontColor = XLColor.FromHtml("#0F5666");

         worksheet.Cell(row, 8).Style.Font.Bold      = true;
         worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#16515F");
         worksheet.Cell(row, 8).Value                = "Invoice Date:";
         worksheet.Cell(row, 9).Value                = data.InvoiceDate.ToString("dd/MM/yyyy");
         row++;

         var address2Range = worksheet.Range(row, 3, row, 6);
         address2Range.Merge();
         address2Range.Value                = data.ClientAddress2;
         address2Range.Style.Font.FontColor = XLColor.FromHtml("#0F5666");

         worksheet.Cell(row, 8).Value =  data.Note;
         row                          += 2;

         worksheet.Cell(row, 2).Style.Font.Bold = true;
         worksheet.Cell(row, 2).Value           = "Client";
         worksheet.Cell(row, 3).Value           = data.ClientShortName;
         row                                    += 2;

         var projects = data.Projects.ToList();
         decimal grandTotal = 0m;
         foreach (var project in projects)
         {
            var lineItems = AggregateLineItems(project.LineItems, data.GroupingType, data.RoundingInterval, data.RoundingType)
               .ToList();

            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value           = "Project";
            worksheet.Cell(row, 3).Value           = project.ProjectName;
            row++;

            worksheet.Cell(row, 2).Style.Font.Bold = true;
            worksheet.Cell(row, 2).Value           = "Time period";
            worksheet.Cell(row, 3).Value           = $"{project.PeriodFrom:dd MMM yyyy} - {project.PeriodTo:dd MMM yyyy}";
            row += 2;

            worksheet.Cell(row, 2).Value = "Week";
            worksheet.Cell(row, 3).Value = "Hours";
            worksheet.Cell(row, 4).Value = "Rate";
            worksheet.Cell(row, 9).Value = "Amount";

            var headerRange = worksheet.Range(row, 2, row, 9);
            headerRange.Style.Font.Bold                     = true;
            headerRange.Style.Font.FontColor                = XLColor.Black;
            headerRange.Style.Border.InsideBorder           = XLBorderStyleValues.Thin;
            headerRange.Style.Border.TopBorder              = XLBorderStyleValues.Medium;
            worksheet.Cell(row, 2).Style.Border.LeftBorder  = XLBorderStyleValues.Medium;
            worksheet.Cell(row, 9).Style.Border.RightBorder = XLBorderStyleValues.Medium;
            headerRange.Style.Alignment.Horizontal          = XLAlignmentHorizontalValues.Center;
            row++;

            var startItemsRow = row;
            decimal projectTotal = 0m;
            foreach (var item in lineItems)
            {
               worksheet.Cell(row, 2).Value                     = item.Description;
               worksheet.Cell(row, 3).Value                     = (double)item.Hours;
               worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.##";

               worksheet.Cell(row, 4).Value                     = item.HourlyRate;
               worksheet.Cell(row, 4).Style.NumberFormat.Format = $"\"{data.Currency}\"#,##0.00";

               worksheet.Cell(row, 9).Value                     = item.Amount;
               worksheet.Cell(row, 9).Style.NumberFormat.Format = $"\"{data.Currency}\"#,##0.00";

               projectTotal                                             += item.Amount;
               worksheet.Range(row, 2, row, 9).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
               row++;
            }

            if (lineItems.Count > 0)
            {
               var endItemsRow = row - 1;
               var itemsRange = worksheet.Range(startItemsRow - 1, 2, endItemsRow, 9);
               itemsRange.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
               itemsRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            }

            worksheet.Cell(row, 8).Value                     = "Total";
            worksheet.Cell(row, 8).Style.Font.Bold           = true;
            worksheet.Cell(row, 9).Value                     = projectTotal;
            worksheet.Cell(row, 9).Style.NumberFormat.Format = $"\"{data.Currency}\"#,##0.00";
            worksheet.Cell(row, 9).Style.Font.Bold           = true;

            var totalRange = worksheet.Range(row, 7, row, 9);
            totalRange.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
            totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            grandTotal += projectTotal;
            row += 2;
         }

         if (projects.Count > 1)
         {
            worksheet.Cell(row, 8).Value                     = "Grand Total";
            worksheet.Cell(row, 8).Style.Font.Bold           = true;
            worksheet.Cell(row, 9).Value                     = grandTotal;
            worksheet.Cell(row, 9).Style.NumberFormat.Format = $"\"{data.Currency}\"#,##0.00";
            worksheet.Cell(row, 9).Style.Font.Bold           = true;

            var grandTotalRange = worksheet.Range(row, 7, row, 9);
            grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#A6DCEA");
            grandTotalRange.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
            grandTotalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
         }

         worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
         worksheet.PageSetup.PaperSize       = XLPaperSize.A4Paper;
         worksheet.PageSetup.Margins.Top     = 1;
         worksheet.PageSetup.Margins.Bottom  = 1;
         worksheet.PageSetup.Margins.Left    = 0.7;
         worksheet.PageSetup.Margins.Right   = 0.7;
         worksheet.Columns("B:I").AdjustToContents(4, 60);

         workbook.SaveAs(filePath);
      }

      private static IEnumerable<InvoiceLineItem> AggregateLineItems(
         IEnumerable<InvoiceLineItem> lineItems,
         GroupingType groupingType,
         RoundingInterval roundingInterval,
         RoundingType roundingType)
      {
         var items = lineItems.ToList();
         return groupingType switch
         {
            GroupingType.NoGrouping => items.Select(item => CreateRoundedItem(
               item.Description,
               item.Hours,
               item.HourlyRate,
               roundingInterval,
               roundingType)),
            GroupingType.GroupBeforeRound => items
               .GroupBy(item => (item.GroupingKey, item.HourlyRate))
               .Select(group => CreateRoundedItem(
                  group.Key.GroupingKey,
                  group.Sum(item => item.Hours),
                  group.Key.HourlyRate,
                  roundingInterval,
                  roundingType)),
            GroupingType.GroupAfterRound => items
               .GroupBy(item => (item.GroupingKey, item.HourlyRate))
               .Select(group => new InvoiceLineItem
               {
                  Description = group.Key.GroupingKey,
                  GroupingKey = group.Key.GroupingKey,
                  Hours = group.Sum(item => (decimal)Round((double)item.Hours, roundingInterval, roundingType)),
                  HourlyRate = group.Key.HourlyRate
               }),
            _ => throw new NotSupportedException()
         };
      }

      private static InvoiceLineItem CreateRoundedItem(
         string description,
         decimal hours,
         decimal hourlyRate,
         RoundingInterval roundingInterval,
         RoundingType roundingType)
      {
         return new InvoiceLineItem
         {
            Description = description,
            GroupingKey = description,
            Hours = (decimal)Round((double)hours, roundingInterval, roundingType),
            HourlyRate = hourlyRate
         };
      }

      private static double Round(in double hours, RoundingInterval roundingInterval, RoundingType roundingType)
      {
         if (roundingInterval == RoundingInterval.RoundTo15Min)
            return roundingType == RoundingType.MidPointRounding
               ? RoundF(hours, 4)
               : CeilingF(hours, 4);

         if (roundingInterval == RoundingInterval.RoundTo30Min)
            return roundingType == RoundingType.MidPointRounding
               ? RoundF(hours, 2)
               : CeilingF(hours, 2);

         return hours;

         static double RoundF(in double value, double factor)
         {
            var rounded = Math.Round(value * factor, MidpointRounding.AwayFromZero) / factor;
            if (Math.Abs(rounded) < 0.001)
               return 1 / factor;
            return rounded;
         }

         static double CeilingF(in double value, double factor)
         {
            var rounded = Math.Ceiling(value * factor) / factor;
            if (Math.Abs(rounded) < 0.001)
               return 1 / factor;
            return rounded;
         }
      }
   }
}
