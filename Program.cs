using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BIXOLON_SamplePg
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CustomAppContext());
        }
    }

    public class CustomAppContext : ApplicationContext
    {
        private BXLAPI.BxlCallBackDelegate statusCallBackDelegate = null;
        private string statusMessage;

        public string GetStatusMessage()
        {
            return statusMessage;
        }

        public CustomAppContext()
        {

            // Load configuration at the start of the application
            var config = ConfigManager.Instance;

            Cursor.Current = Cursors.WaitCursor;
            Cursor.Current = Cursors.Default;

            Console.WriteLine($"API URL: {config.ApiUrl}");
            Console.WriteLine($"Printer Name: {config.PrinterName}");
            Console.WriteLine($"Printer ID: {config.PrinterId}");

            RequestPrinterStatus();

        }

        private void RequestPrinterStatus()
        {
            string strStatus = "";

            /////////////////////FOR USB CONNECTION////////////////////////////
            if (BXLAPI.PrinterOpen(BXLAPI.IUsb, "", 0, 0, 0, 0, 0) != BXLAPI.BXL_SUCCESS)
            {
               strStatus = "Connect fail [USB]";
               Console.WriteLine(strStatus);
              return;
            }

            /////////////////////FOR SERIAL CONNECTION////////////////////////////
            //string strPort = "COM4";
            //int nBaudrate = 115200, nDatabits = 8, nParity = 0, nStopbits = 0, nFlowControl = 1;

            //if (BXLAPI.PrinterOpen(BXLAPI.ISerial, strPort, nBaudrate, nDatabits, nParity, nStopbits, nFlowControl) != BXLAPI.BXL_SUCCESS)
            //{
            //   strStatus = "Connect fail [Serial]";
            //    Console.WriteLine(strStatus);
            //    return;
           // }
            else
            {
                statusCallBackDelegate = new BXLAPI.BxlCallBackDelegate(StatusCallBackMethod);

                if (BXLAPI.BidiSetCallBack(statusCallBackDelegate) != BXLAPI.BXL_SUCCESS)
                {
                    statusCallBackDelegate = null;
                }
            }
        }

        private async Task UpdateStatusToApiAsync(int id, string printerName, string status)
        {
            string apiUrl = ConfigManager.Instance.ApiUrl + "/set-status";
            Console.WriteLine(apiUrl);
            Console.WriteLine(id);
            Console.WriteLine(printerName);
            Console.WriteLine(status);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var payload = new[]
                    {
                new
                {
                    id = id,
                    printer_name = printerName,
                    status = status,
                    lastChecked = DateTime.Now
                }
            };

                    string jsonPayload = JsonSerializer.Serialize(payload);
                    HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();

                    Console.WriteLine("Status updated successfully.");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error updating status: {ex.Message}");
                }
            }
        }

        public int StatusCallBackMethod(int status)
        {
            string strStatus = "";
            Console.WriteLine("Returning status: " + status);

            if (status == BXLAPI.BXL_STS_NORMAL)
                strStatus += " - NORMAL" + "\r\n";
            if ((status & BXLAPI.BXL_STS_PAPER_NEAR_END) == BXLAPI.BXL_STS_PAPER_NEAR_END)
                strStatus += " - PAPER-NEAR-END" + "\r\n";
            if ((status & BXLAPI.BXL_STS_PAPEREMPTY) == BXLAPI.BXL_STS_PAPEREMPTY)
                strStatus += " - PAPER-EMPTY" + "\r\n";
            if ((status & BXLAPI.BXL_STS_CASHDRAWER_HIGH) == BXLAPI.BXL_STS_CASHDRAWER_HIGH)
                strStatus += " - CASHDRAWER-HIGH" + "\r\n";
            if ((status & BXLAPI.BXL_STS_CASHDRAWER_LOW) == BXLAPI.BXL_STS_CASHDRAWER_LOW)
                strStatus += " - CASHDRAWER-LOW" + "\r\n";
            if ((status & BXLAPI.BXL_STS_COVEROPEN) == BXLAPI.BXL_STS_COVEROPEN)
                strStatus += " - COVER-OPEN" + "\r\n";
            if ((status & BXLAPI.BXL_STS_ERROR) == BXLAPI.BXL_STS_ERROR)
                strStatus += " - OFFLINE" + "\r\n";
            if ((status & BXLAPI.BXL_STS_BATTERY_LOW) == BXLAPI.BXL_STS_BATTERY_LOW)
                strStatus += " - BATTERY-LOW" + "\r\n";
            if ((status & BXLAPI.BXL_STS_PAPER_TO_BE_TAKEN) == BXLAPI.BXL_STS_PAPER_TO_BE_TAKEN)
                strStatus += " - PAPER-PRESENCE" + "\r\n";

            Console.WriteLine(strStatus);

            statusMessage = strStatus;
            // Trigger API update
            _ = UpdateStatusToApiAsync(ConfigManager.Instance.PrinterId, ConfigManager.Instance.PrinterName, strStatus);
            //_ = UpdateStatusToApiAsync(ConfigManager.Instance.PrinterId, ConfigManager.Instance.PrinterName, strStatus).ConfigureAwait(false);

            return 0;
        }

        public void StopStatusCallback()
        {
            BXLAPI.BidiCancelCallBack();
            statusCallBackDelegate = null;
        }
    }
}
