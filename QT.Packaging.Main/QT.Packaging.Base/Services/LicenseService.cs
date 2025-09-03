using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base.Services
{
    public class LicenseService
    {
        private readonly IPlatformService _platformService;

        public LicenseService(IPlatformService platformService)
        {
            _platformService = platformService;
        }

        public bool CheckLicense()
        {
            string machineCode = _platformService.GetMachineCode();
            string storedLicenseKey = "";//Preferences.Get("LicenseKey", string.Empty);
            return CheckLicense(machineCode, storedLicenseKey);
        }

        public bool CheckLicense(string machineCode, string storedLicenseKey)
        {
            if (string.IsNullOrEmpty(storedLicenseKey) || !LicenseService.ValidateLicenseKey(machineCode, storedLicenseKey))
            {
                return false; // 验证失败
            }

            return true; // 验证成功
        }

        public static bool ValidateLicenseKey(string machineCode, string licenseKey)
        {
            using var rsa = RSA.Create();
            // 加载公钥（从文件或硬编码）
            try
            {


                rsa.ImportFromPem(@"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAq8XAhF0lbdBXLpVfbE7R
oOBSkPZd8Rg3oPPTy/hYx7/Zd09Rm4PdR1rXNL4PAMWK+9wqjcHKXs6aHMu/2lbP
HdDA7PgWehA9hRhHCtUWhcTxbrtGPq0DFkkLOQKrWmzCZ41vXv9oQ6poAnfaMluU
6MxsOoWw3AtRb4wt8zyWz+chkyz0eYLGmkzdpfLKWohbWcWqgF/6FOAXGabJzwNt
HvDxMEzVAqWSXUQicY88/XmHkFq3/m1+xNbakTqRmuznEt37v8YKyvuGwtrqBFpW
EeUKWtXSavHQQ7o80ZyuZG88gm7eOCC/K5zGMPB25nVYsrX/29GepFr43GQdTuPi
iwIDAQAB
-----END PUBLIC KEY-----");
            }
            catch (Exception)
            {
                //AlertHelper.ShowAlertAsync("注册失败", "注册码无效，请重新注册！", "OK");
                Environment.Exit(0); // 退出程序
                return false;
            }
            // 验证签名
            byte[] machineCodeBytes = Encoding.UTF8.GetBytes(machineCode);
            byte[] licenseKeyBytes = Convert.FromBase64String(licenseKey);

            return rsa.VerifyData(machineCodeBytes, licenseKeyBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public void HandleInvalidLicense()
        {
            //AlertHelper.ShowAlertAsync("注册失败", "注册码无效，请重新注册！", "OK");
            Environment.Exit(0); // 退出程序
        }
    }
}
