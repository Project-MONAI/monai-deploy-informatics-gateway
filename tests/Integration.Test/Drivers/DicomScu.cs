using System.Diagnostics;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    public class DicomScu
    {
        private static readonly object SyncRoot = new object();
        internal int TotalTime { get; private set; } = 0;

        private readonly ISpecFlowOutputHelper _outputHelper;

        public DicomScu(ISpecFlowOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public async Task<DicomStatus> CEcho(string host, int port, string callingAeTitle, string calledAeTitle, TimeSpan timeout)
        {
            _outputHelper.WriteLine($"C-ECHO: {callingAeTitle} => {host}:{port}@{calledAeTitle}");
            var result = DicomStatus.Pending;
            var dicomClient = CreateClient(host, port, callingAeTitle, calledAeTitle);

            var cEchoRequest = new DicomCEchoRequest();
            var manualReset = new ManualResetEvent(false);
            cEchoRequest.OnResponseReceived += (DicomCEchoRequest request, DicomCEchoResponse response) =>
            {
                result = response.Status;
                manualReset.Set();
            };
            await dicomClient.AddRequestAsync(cEchoRequest);

            try
            {
                await dicomClient.SendAsync();
                manualReset.WaitOne(timeout);
                return result;
            }
            catch (DicomAssociationRejectedException ex)
            {
                Console.WriteLine("Association Rejected: {0}", ex.Message);
                return DicomStatus.Cancel;
            }
        }

        public async Task<DicomStatus> CStore(string host, int port, string callingAeTitle, string calledAeTitle, IList<DicomFile> dicomFiles, TimeSpan timeout)
        {
            _outputHelper.WriteLine($"C-STORE: {callingAeTitle} => {host}:{port}@{calledAeTitle}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var dicomClient = CreateClient(host, port, callingAeTitle, calledAeTitle);
            var countdownEvent = new CountdownEvent(dicomFiles.Count);
            var failureStatus = new List<DicomStatus>();
            foreach (var file in dicomFiles)
            {
                // _outputHelper.WriteLine("Sending DICOM Instance: PID={0}, STUDY={1}, SOP={2}",
                //     file.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, "N/A"),
                //     file.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "N/A"),
                //     file.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, "N/A"));

                var cStoreRequest = new DicomCStoreRequest(file);
                cStoreRequest.OnResponseReceived += (DicomCStoreRequest request, DicomCStoreResponse response) =>
                {
                    if (response.Status != DicomStatus.Success) failureStatus.Add(response.Status);
                    countdownEvent.Signal();
                };
                await dicomClient.AddRequestAsync(cStoreRequest);
            }

            try
            {
                await dicomClient.SendAsync();
                countdownEvent.Wait(timeout);
                stopwatch.Stop();
                lock (SyncRoot)
                {
                    TotalTime += (int)stopwatch.Elapsed.TotalMilliseconds;
                }
                _outputHelper.WriteLine($"DICOMsend:{stopwatch.Elapsed.TotalSeconds}s");
            }
            catch (DicomAssociationRejectedException ex)
            {
                _outputHelper.WriteLine($"Association Rejected: {ex.Message}");
                return DicomStatus.Cancel;
            }

            if (failureStatus.Count == 0) return DicomStatus.Success;

            return failureStatus.First();
        }

        private IDicomClient CreateClient(string host, int port, string callingAeTitle, string calledAeTitle)
        {
            return DicomClientFactory.Create(host, port, false, callingAeTitle, calledAeTitle);
        }
    }
}
