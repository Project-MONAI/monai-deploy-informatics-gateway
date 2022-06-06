// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    [Binding]
    public sealed class DicomWebStowHooks
    {
        [BeforeScenario("@dicomweb_stow")]
        public void DicomWebStow(ScenarioContext scenarioContext)
        {
            scenarioContext[SharedDefinitions.KeyDataGrouping] = "stow_none";
        }

        [BeforeScenario("@dicomweb_stow_study")]
        public void DicomWebStow_WithStudyInstanceUid(ScenarioContext scenarioContext)
        {
            scenarioContext[SharedDefinitions.KeyDataGrouping] = "stow_study";
        }
    }
}
