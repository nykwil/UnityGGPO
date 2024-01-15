using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityGGPO {

    public static class Utils {

        public static int CalcFletcher32(NativeArray<byte> data) {
            uint sum1 = 0;
            uint sum2 = 0;

            int index;
            for (index = 0; index < data.Length; ++index) {
                sum1 = (sum1 + data[index]) % 0xffff;
                sum2 = (sum2 + sum1) % 0xffff;
            }
            return unchecked((int)((sum2 << 16) | sum1));
        }

        public static string GetString(IntPtr ptrStr) {
            return ptrStr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptrStr) : "";
        }

        public static unsafe void* ToPtr(NativeArray<byte> data) {
            unsafe {
                return NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
            }
        }

        public static unsafe NativeArray<byte> ToArray(void* dataPointer, int length) {
            unsafe {
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(dataPointer, length, Allocator.Persistent);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());
#endif
                return array;
            }
        }

        public static int nmod(int n, int by) {
            while (n < 0) {
                n += by;
            }
            while (n >= by) {
                n -= by;
            }
            return n;
        }


        public static int TimeGetTime() {
            return UnityGGPO.GGPO.UggTimeGetTime();
        }

        public static void Sleep(int ms) {
            UnityGGPO.GGPO.UggSleep(ms);
        }
    }
}