using System.Numerics;
using Application.Interfaces;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

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
          { ""internalType"": ""uint256"", ""name"": ""invoiceId"", ""type"": ""uint256"" },
          { ""internalType"": ""string"", ""name"": ""hash"", ""type"": ""string"" }
        ],
        ""name"": ""storeInvoiceHash"",
        ""outputs"": [],
        ""stateMutability"": ""nonpayable"",
        ""type"": ""function""
      },
      {
        ""inputs"": [
          { ""internalType"": ""uint256"", ""name"": ""invoiceId"", ""type"": ""uint256"" }
        ],
        ""name"": ""getInvoiceHash"",
        ""outputs"": [
          { ""internalType"": ""string"", ""name"": """", ""type"": ""string"" }
        ],
        ""stateMutability"": ""view"",
        ""type"": ""function""
      }
    ]";
    
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

    public async Task<string> PushInvoiceHashAsync(Guid invoiceId, string hash)
    {
        var uintInvoiceId = GuidToUInt256(invoiceId);
        var contract = _web3.Eth.GetContract(ABI, _contractAddress);
        var storeFunction = contract.GetFunction("storeInvoiceHash");
        var txHash = await storeFunction.SendTransactionAsync(
            _web3.TransactionManager.Account.Address,
            new HexBigInteger(300000),
            new HexBigInteger(0),
            uintInvoiceId, // convert Guid -> uint hoặc hash
            hash
        );
        return txHash;
    }

    public async Task<string> GetInvoiceHashAsync(Guid invoiceId)
    {
        var contract = _web3.Eth.GetContract(ABI, _contractAddress);
        var getFunction = contract.GetFunction("getInvoiceHash");
        return await getFunction.CallAsync<string>(invoiceId.GetHashCode());
    }
}