﻿using System;
using System.IO;
using System.Threading.Tasks;
using validatequotes.Helpers;

namespace validatequotes
{
    public class Program
    {
        private string fileName;
        private string attestDnsName;
        private bool includeDetails;

        public static void Main(string[] args)
        {
            Task.WaitAll(new Program(args).RunAsync());
        }

        public Program(string[] args)
        {
            this.fileName = args.Length > 0 ? args[0] : "../../../genquotes/quotes/enclave.info.debug.json";
            this.attestDnsName = args.Length > 1 ? args[1] : "uksgnkdemo1.uks.test.attest.azure.net";
            this.includeDetails = true;
            if (args.Length > 2)
            {
                bool.TryParse(args[2], out this.includeDetails);
            }

            if ((args.Length == 1) && (args[0].Contains("-u")))
            {
                Logger.WriteBanner($"USAGE");
                Logger.WriteLine($"Usage: dotnet validatequotes.core.dll <JSON file name> <attest DNS name> <include details bool>");
                Logger.WriteLine($"Usage: dotnet run                     <JSON file name> <attest DNS name> <include details bool>");
                Logger.WriteLine($" - validates remote attestation quotes generated by genquote application");
                Logger.WriteLine($" - validates via calling the OE attestation endpoint on the MAA service");
                System.Environment.Exit(0);
            }

            Logger.WriteBanner($"PARAMETERS FOR THIS RUN");
            Logger.WriteLine($"Validating filename                : {this.fileName}");
            Logger.WriteLine($"Using attestation provider         : {this.attestDnsName}");
            Logger.WriteLine($"Including details                  : {this.includeDetails}");
        }

        public async Task RunAsync()
        {
            // Fetch file
            var enclaveInfo = EnclaveInfo.CreateFromFile(this.fileName);

            // Send to service for attestation
            var maaService = new MaaService(this.attestDnsName);
            var serviceJwtToken = await maaService.AttestOpenEnclaveAsync(enclaveInfo.GetMaaBody());

            // Analyze results
            Logger.WriteBanner("VALIDATING MAA JWT TOKEN - BASICS");
            JwtValidationHelper.ValidateMaaJwt(attestDnsName, serviceJwtToken, this.includeDetails);

            Logger.WriteBanner("VALIDATING MAA JWT TOKEN - CLAIMS MATCH CLIENT ENCLAVE INFO");
            enclaveInfo.CompareToMaaServiceJwtToken(serviceJwtToken, this.includeDetails);

            Logger.WriteLine("\n\n");
        }
    }
}
