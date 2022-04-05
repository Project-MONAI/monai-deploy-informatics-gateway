// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    [Binding]
    public static class ScpHooks
    {
        private static IDicomServer s_dicomServer;

        internal static readonly string KeyServerData = "SERVER-DATA";
        internal static readonly string FeatureScpAeTitle = "TEST-SCP";
        internal static readonly int FeatureScpPort = 1105;

        [BeforeFeature("@scp")]
        public static void BeforeMessagingExportComplete(ISpecFlowOutputHelper outputHelper, FeatureContext featureContext)
        {
            try
            {
                var data = new ServerData { Context = featureContext, OutputHelper = outputHelper };
                s_dicomServer = DicomServerFactory.Create<CStoreScp>(FeatureScpPort, userState: data);
                featureContext[KeyServerData] = data;
            }
            catch (Exception ex)
            {
                outputHelper.WriteLine("Exception while starting DICOM SCP Listener: {0}", ex);
            }
        }

        [AfterFeature("@scp")]
        public static void AfterScenario(ISpecFlowOutputHelper outputHelper, FeatureContext featureContext)
        {
            try
            {
                featureContext.Remove(KeyServerData);
                s_dicomServer?.Stop();
                s_dicomServer?.Dispose();
            }
            catch (Exception ex)
            {
                outputHelper.WriteLine("Exception while stopping DICOM SCP Listener: {0}", ex);
            }
        }
    }
}
