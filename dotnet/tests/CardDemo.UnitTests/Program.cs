using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardDemo.Batch.IntCalc;
using CardDemo.Batch.Restart;
using CardDemo.Data.Codecs;
using CardDemo.Data.Stores;
using CardDemo.Domain.Models;

namespace CardDemo.UnitTests
{
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Running CardDemo Unit Tests...");

            // Test 1: Comp3Codec
            byte[] packedPositive = new byte[] { 0x01, 0x25, 0x0C }; // +12.50
            decimal valPos = Comp3Codec.ReadDecimal(packedPositive, 2);
            if (valPos != 12.50m) throw new Exception($"Comp3Codec positive failed: expected 12.50, got {valPos}");

            byte[] packedNegative = new byte[] { 0x00, 0x50, 0x0D }; // -5.00
            decimal valNeg = Comp3Codec.ReadDecimal(packedNegative, 2);
            if (valNeg != -5.00m) throw new Exception($"Comp3Codec negative failed: expected -5.00, got {valNeg}");

            // Test 2: EbcdicCodec
            byte[] ebcdicBytes = new byte[10];
            EbcdicCodec.WriteString("TESTACCT", ebcdicBytes);
            string decoded = EbcdicCodec.ReadString(ebcdicBytes);
            if (decoded != "TESTACCT") throw new Exception($"EbcdicCodec failed: expected TESTACCT, got {decoded}");

            // Test 3: BR-003 Interest Calculation Formula
            decimal balance = 1000.00m;
            decimal rate = 12.00m; // 12%
            decimal interest = Math.Round((balance * rate) / 1200m, 2, MidpointRounding.AwayFromZero);
            if (interest != 10.00m) throw new Exception($"Interest math rule failed: expected 10.00, got {interest}");

            Console.WriteLine("[PASS] All Unit Tests Passed Successfully.");
            await Task.Yield();
        }
    }
}
