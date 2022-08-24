using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var patient = new Patient()
        {
            Name = new List<HumanName>()
                {
                    new HumanName()
                    {
                        Given = new List<string>{"John","James" },
                        Family = "Doe"
                    }
                },
            Gender = AdministrativeGender.Male,
            BirthDate = "1990-01-01",
            Identifier = new List<Identifier>()
                {
                    new Identifier()
                    {
                        Value = "123456790"
                    }
                },
            Active = true,
            Contact = new List<Patient.ContactComponent>
            {
                new Patient.ContactComponent
                {
                    Name = new  HumanName{ Given = new [] { "Awesome"}, Family = "Super"},
                     Address = new Address
                     {
                          City = "Somewhere",
                           Country = "YAY"
                     }
                }
            }
        };

        try
        {
            //var client = new FhirClient("http://server.fire.ly/Consent");
            var client = new FhirClient("http://localhost:5000/fhir");
            client.Settings.PreferredFormat = ResourceFormat.Xml;
            var result = client.Create(patient);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
