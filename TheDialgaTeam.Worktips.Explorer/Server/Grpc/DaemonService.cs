using System.Text.Json;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Cryptonote.Rpc.Worktips;
using TheDialgaTeam.Cryptonote.Rpc.Worktips.Json.Daemon;
using TheDialgaTeam.Worktips.Explorer.Server.Database;
using TheDialgaTeam.Worktips.Explorer.Server.Options;
using TheDialgaTeam.Worktips.Explorer.Shared.Models;

namespace TheDialgaTeam.Worktips.Explorer.Server.Grpc;

public class DaemonService : Daemon.DaemonBase
{
    private readonly DaemonRpcClient _daemonRpcClient;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;
    private readonly IOptionsSnapshot<BlockchainOptions> _blockchainOptions;

    public DaemonService(DaemonRpcClient daemonRpcClient, IDbContextFactory<SqliteDatabaseContext> dbContextFactory, IOptionsSnapshot<BlockchainOptions> blockchainOptions)
    {
        _daemonRpcClient = daemonRpcClient;
        _dbContextFactory = dbContextFactory;
        _blockchainOptions = blockchainOptions;
    }

    public override Task<GetCoinInfoResponse> GetCoinInfo(NoParametersRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetCoinInfoResponse
        {
            Success = true,
            Ticker = _blockchainOptions.Value.CoinTicker,
            Unit = _blockchainOptions.Value.CoinUnit
        });
    }

    public override async Task<GetCurrentDaemonInfoResponse> GetCurrentDaemonInfo(NoParametersRequest request, ServerCallContext context)
    {
        try
        {
            var infoResponse = await _daemonRpcClient.GetInfoAsync();
            if (infoResponse == null) return new GetCurrentDaemonInfoResponse { Success = false };

            var blockHeaderByHeightResponse = await _daemonRpcClient.GetBlockHeaderByHeightAsync(new CommandRpcGetBlockHeaderByHeight.Request
            {
                Height = infoResponse.Height - 1, 
                FillPowHash = false
            });
            if (blockHeaderByHeightResponse == null) return new GetCurrentDaemonInfoResponse { Success = false };

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var daemonSyncHistory = await dbContext.DaemonSyncHistory.FirstAsync();

            return new GetCurrentDaemonInfoResponse
            {
                Success = true,
                Height = infoResponse.Height,
                Difficulty = infoResponse.Difficulty,
                NetworkHashRate = (double) infoResponse.Difficulty / infoResponse.Target,
                BlockReward = blockHeaderByHeightResponse.BlockHeader.Reward,
                TotalTransactions = infoResponse.TransactionCount,
                CirculatingSupply = daemonSyncHistory.TotalCirculation,
                TotalSupply = _blockchainOptions.Value.CoinMaxSupply,
                CurrentEmission = (double) daemonSyncHistory.TotalCirculation / _blockchainOptions.Value.CoinMaxSupply
            };
        }
        catch (Exception)
        {
            return new GetCurrentDaemonInfoResponse { Success = false };
        }
    }

    public override async Task<GetChartDataResponse> GetChartData(NoParametersRequest request, ServerCallContext context)
    {
        try
        {
            var heightResponse = await _daemonRpcClient.GetHeightAsync();
            if (heightResponse == null) return new GetChartDataResponse { Success = false };

            var targetHeight = heightResponse.Height - 1;

            var response = await _daemonRpcClient.GetBlockHeadersRangeAsync(new CommandRpcGetBlockHeadersRange.Request
            {
                StartHeight = targetHeight - 49,
                EndHeight = targetHeight,
                FillPowHash = false
            });
            if (response == null) return new GetChartDataResponse { Success = false };

            var chartData = new List<GetChartDataResponse.Types.ChartData>();

            foreach (var blockHeaderResponse in response.Headers)
            {
                chartData.Add(new GetChartDataResponse.Types.ChartData
                {
                    Timestamp = blockHeaderResponse.Timestamp,
                    Difficulty = blockHeaderResponse.Difficulty,
                    BlockSize = blockHeaderResponse.BlockSize,
                    TxCount = blockHeaderResponse.TransactionsCount
                });
            }

            return new GetChartDataResponse
            {
                Success = true,
                ChartData = { chartData }
            };
        }
        catch (Exception)
        {
            return new GetChartDataResponse { Success = false };
        }
    }

    public override async Task<GetTransactionPoolResponse> GetTransactionPool(NoParametersRequest request, ServerCallContext context)
    {
        try
        {
            var response = await _daemonRpcClient.GetTransactionPoolAsync();
            if (response == null) return new GetTransactionPoolResponse { Success = false };

            var transactions = new List<GetTransactionPoolResponse.Types.Transaction>();

            if (response.Transactions != null)
            {
                foreach (var responseTransaction in response.Transactions)
                {
                    transactions.Add(new GetTransactionPoolResponse.Types.Transaction
                    {
                        Age = (ulong) (DateTimeOffset.Now - DateTimeOffset.FromUnixTimeSeconds((long) responseTransaction.ReceiveTime)).TotalSeconds,
                        Fee = responseTransaction.Fee,
                        Size = responseTransaction.BlobSize,
                        Hash = responseTransaction.IdHash
                    });
                }
            }

            return new GetTransactionPoolResponse
            {
                Success = true,
                Transactions = { transactions }
            };
        }
        catch (Exception)
        {
            return new GetTransactionPoolResponse { Success = false };
        }
    }

    public override async Task<GetRecentBlocksResponse> GetRecentBlocks(NoParametersRequest request, ServerCallContext context)
    {
        try
        {
            var heightResponse = await _daemonRpcClient.GetHeightAsync();
            if (heightResponse == null) return new GetRecentBlocksResponse { Success = false };

            var targetHeight = heightResponse.Height - 1;

            var response = await _daemonRpcClient.GetBlockHeadersRangeAsync(new CommandRpcGetBlockHeadersRange.Request
            {
                StartHeight = targetHeight - 49,
                EndHeight = targetHeight,
                FillPowHash = false
            });
            if (response == null) return new GetRecentBlocksResponse { Success = false };

            var blocks = new List<GetRecentBlocksResponse.Types.Block>();

            foreach (var blockHeaderResponse in response.Headers.Reverse())
            {
                blocks.Add(new GetRecentBlocksResponse.Types.Block
                {
                    Height = blockHeaderResponse.Height,
                    Size = blockHeaderResponse.BlockSize,
                    Hash = blockHeaderResponse.Hash,
                    Difficulty = blockHeaderResponse.Difficulty,
                    TxCount = blockHeaderResponse.TransactionsCount,
                    Timestamp = blockHeaderResponse.Timestamp
                });
            }

            return new GetRecentBlocksResponse
            {
                Success = true,
                Blocks = { blocks }
            };
        }
        catch (Exception)
        {
            return new GetRecentBlocksResponse { Success = false };
        }
    }

    public override async Task<GetBlockInfoResponse> GetBlockInfo(GetBlockInfoRequest request, ServerCallContext context)
    {
        try
        {
            CommandRpcGetBlock.Request commandRpcGetBlockRequest;

            switch (request.BlockTypeCase)
            {
                case GetBlockInfoRequest.BlockTypeOneofCase.Height:
                    commandRpcGetBlockRequest = new CommandRpcGetBlock.Request { Height = request.Height };
                    break;

                case GetBlockInfoRequest.BlockTypeOneofCase.Hash:
                    commandRpcGetBlockRequest = new CommandRpcGetBlock.Request { Hash = request.Hash };
                    break;

                default:
                    return new GetBlockInfoResponse { Success = false };
            }

            var response = await _daemonRpcClient.GetBlockAsync(commandRpcGetBlockRequest);
            if (response == null) return new GetBlockInfoResponse { Success = false };

            var transactionsHashes = new List<string> { response.BlockHeader.MinerTransactionHash };
            if (response.TransactionHashes != null) transactionsHashes.AddRange(response.TransactionHashes);

            var transactionResponse = await _daemonRpcClient.GetTransactionsAsync(new CommandRpcGetTransactions.Request
            {
                TransactionHashes = transactionsHashes.ToArray(),
                DecodeAsJson = true
            });
            if (transactionResponse == null) return new GetBlockInfoResponse { Success = false };

            var txSize = 0.0;
            var txFee = 0ul;

            var transactions = new List<GetBlockInfoResponse.Types.TransactionInfo>();

            for (var i = 0; i < transactionsHashes.Count; i++)
            {
                var transaction = JsonSerializer.Deserialize(transactionResponse.Transactions[i].AsJson, TransactionModelContext.Default.TransactionModel);
                if (transaction == null) return new GetBlockInfoResponse { Success = false };

                transactions.Add(new GetBlockInfoResponse.Types.TransactionInfo
                {
                    Hash = transactionsHashes[i],
                    Fee = transaction.RingSignatures.TransactionFee,
                    TotalAmount = (ulong) transaction.Vout.Sum(vout => (decimal) vout.Amount),
                    Size = transactionResponse.Transactions[i].AsHex.Length / 2.0
                });

                txFee += transaction.RingSignatures.TransactionFee;
                txSize += transactionResponse.Transactions[i].AsHex.Length / 2.0;
            }

            return new GetBlockInfoResponse
            {
                Success = true,
                Hash = response.BlockHeader.Hash,
                Height = response.BlockHeader.Height,
                Confirmation = response.BlockHeader.Depth,
                Timestamp = response.BlockHeader.Timestamp,
                Version = $"{response.BlockHeader.MajorVersion}.{response.BlockHeader.MinorVersion}",
                Difficulty = response.BlockHeader.Difficulty,
                Nonce = response.BlockHeader.Nonce,
                Size = response.BlockHeader.BlockSize,
                MinerReward = response.BlockHeader.MinerReward,
                Reward = response.BlockHeader.Reward,
                TxSize = txSize,
                TxFee = txFee,
                PreviousBlockHash = response.BlockHeader.PreviousHash,
                ServiceNodeWinner = response.BlockHeader.ServiceNodeWinner,
                TransactionInfo = { transactions }
            };
        }
        catch (Exception)
        {
            return new GetBlockInfoResponse { Success = false };
        }
    }
}
