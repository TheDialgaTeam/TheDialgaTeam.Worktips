﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TheDialgaTeam.Worktips.Explorer.Server.Database;

#nullable disable

namespace TheDialgaTeam.Worktips.Explorer.Server.Migrations
{
    [DbContext(typeof(SqliteDatabaseContext))]
    partial class SqliteDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");

            modelBuilder.Entity("TheDialgaTeam.Worktips.Explorer.Server.Database.Tables.DaemonSyncHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("BlockCount")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("TotalCirculation")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("DaemonSyncHistory");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            BlockCount = 0ul,
                            TotalCirculation = 0ul
                        });
                });

            modelBuilder.Entity("TheDialgaTeam.Worktips.Explorer.Server.Database.Tables.FaucetHistory", b =>
                {
                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("DateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.ToTable("FaucetHistories");
                });

            modelBuilder.Entity("TheDialgaTeam.Worktips.Explorer.Server.Database.Tables.WalletAccount", b =>
                {
                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("AccountIndex")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RegisteredWalletAddress")
                        .HasMaxLength(98)
                        .HasColumnType("TEXT");

                    b.Property<bool>("SentToRegisteredWalletDirectly")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TipBotWalletAddress")
                        .IsRequired()
                        .HasMaxLength(98)
                        .HasColumnType("TEXT");

                    b.HasKey("UserId");

                    b.ToTable("WalletAccounts");
                });
#pragma warning restore 612, 618
        }
    }
}
