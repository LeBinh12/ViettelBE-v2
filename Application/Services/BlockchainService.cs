using System.Numerics;
using Application.DTOs;
using Application.Interfaces;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Share;

namespace Application.Services;

public class BlockchainService : IBlockchainService
{
    private readonly Web3 _web3;
    private readonly string _contractAddress; // 0xd9145CCE52D386f254917e481eB44e9943F39138
    private readonly string _senderPrivateKey;
    // _senderPrivateKey =  2a1a4a534cceafbe4eef95d3b9ea502730edac8bc629ec3a9c8194e1ff0a561b
    // RPC = https://mainnet.infura.io/v3/13c2c1b2df36470898405a574a72ac65
    
    private const string ABI = @"[
  {
    ""inputs"": [
      { ""internalType"": ""bytes32"", ""name"": ""invoiceId"", ""type"": ""bytes32"" },
      { ""internalType"": ""bytes32"", ""name"": ""hashValue"", ""type"": ""bytes32"" },
      { ""internalType"": ""string"", ""name"": ""note"", ""type"": ""string"" }
    ],
    ""name"": ""storeInvoiceHash"",
    ""outputs"": [],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      { ""internalType"": ""bytes32"", ""name"": ""invoiceId"", ""type"": ""bytes32"" }
    ],
    ""name"": ""getLatestInvoiceHash"",
    ""outputs"": [
      { ""internalType"": ""bytes32"", ""name"": """", ""type"": ""bytes32"" },
      { ""internalType"": ""bool"", ""name"": """", ""type"": ""bool"" }
    ],
    ""stateMutability"": ""view"",
    ""type"": ""function""
  }
]
";

    
    private static BigInteger GuidToUInt256(Guid guid)
    {
        var bytes = guid.ToByteArray();
        // Thêm 0 byte ở cuối để tránh âm (sign bit)
        var extended = new byte[bytes.Length + 1];
        Array.Copy(bytes, extended, bytes.Length);
        return new BigInteger(extended);
    }

    public BlockchainService(string rpcUrl, string contractAddress, string senderPrivateKey)
    {
        _web3 = new Web3(new Nethereum.Web3.Accounts.Account(senderPrivateKey), rpcUrl);
        _contractAddress = contractAddress;
        _senderPrivateKey = senderPrivateKey;
    }

    public async Task<string> PushInvoiceHashAsync(Guid invoiceId, string hashValue)
    {
        var contract = _web3.Eth.GetContract(ABI, _contractAddress);
        var storeFunction = contract.GetFunction("storeInvoiceHash");

        byte[] invoiceKey = GuidToBytes32(invoiceId);
        byte[] hashBytes = GuidConverter.HexStringToBytes32(hashValue);

        var txHash = await storeFunction.SendTransactionAsync(
            _web3.TransactionManager.Account.Address,
            new HexBigInteger(300000),
            new HexBigInteger(0),
            invoiceKey,
            hashBytes,
            "PAID"
        );

        var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
        while (receipt == null)
        {
            await Task.Delay(500);
            receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
        }

        return txHash;
    }


    public async Task<string?> GetInvoiceHashAsync(Guid invoiceId)
    {
        var contract = _web3.Eth.GetContract(ABI, _contractAddress);
        var getFn = contract.GetFunction("getLatestInvoiceHash");

        byte[] invoiceKey = GuidToBytes32(invoiceId);

        var result = await getFn.CallDeserializingToObjectAsync<GetLatestInvoiceHashOutputDTO>(invoiceKey);

        if (result == null || !result.Exists)
            return null;

        return result.Hash.ToHex();
    }

    public static byte[] GuidToBytes32(Guid guid)
    {
        byte[] guidBytes = guid.ToByteArray(); // 16 bytes
        byte[] bytes32 = new byte[32];
        Buffer.BlockCopy(guidBytes, 0, bytes32, 0, 16); 
        return bytes32;
    }
    
    private static byte[] HexStringToBytes32(string hex)
    {
        var bytes = hex.HexToByteArray();
        if (bytes.Length != 32)
            throw new Exception("Hash must be exactly 32 bytes for bytes32!");
        return bytes;
    }
}