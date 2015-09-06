using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

using LockTest.Tests;
using LockTest.Synchronization;

namespace LockTest
{
    class Program
    {
        static int Main(string[] args)
        {
            testParameters = new TestParameters();

            testParameters.MaxWaitSeconds = 5;
            
            Type[] lockTypes = new Type[]
            {
                typeof(NoOpLock),
                typeof(SimpleSpinLock),
                typeof(LockTest.Synchronization.SpinLock),
                typeof(Lock)
            };

            TestSpecification[] testSpecifications = new TestSpecification[]
            {
                new TestSpecification(typeof(UnbalancedCallTest), "Unbalanced calls"),
                new TestSpecification(typeof(AutoLockTest), "Automatic lock management"),
                new TestSpecification(typeof(ConcurrencyTest), ConcurrencyTest.Variant.Default, "Concurrent access - 2 threads"),
                new TestSpecification(typeof(ConcurrencyTest), ConcurrencyTest.Variant.ObjectLock, "Concurrent access - 2 threads"),
                new TestSpecification(typeof(ConcurrencyTest), ConcurrencyTest.Variant.ManyThreads, "Concurrent access - many threads"),
                new TestSpecification(typeof(ConcurrencyTest), ConcurrencyTest.Variant.BoundedWait, "Concurrent access - bounded wait sanity"),
                new TestSpecification(typeof(TimeoutTest), "Waits with maximum timeouts")
            };

            if (args.Length > 0 && args[0] == "-?")
            {
                PrintUsage(lockTypes, testSpecifications);
                return -1;
            }

            int argStart = 0;

            if (args.Length > 0)
            {
                if (args[0] == "-t")
                {
                    if (args.Length > 1)
                    {
                        testParameters.MaxWaitSeconds = int.Parse(args[1]);
                    }
                    else
                    {
                        PrintUsage(lockTypes, testSpecifications);
                        return -1;
                    }

                    argStart+=2;
                }
            }

            bool[] variationMask = null;

            int variationsIncluded = lockTypes.Length * testSpecifications.Length;

            if (argStart < args.Length)
            {
                variationsIncluded = 0;
                variationMask = new bool[lockTypes.Length * testSpecifications.Length];

                for (int argumentIndex = argStart; argumentIndex < args.Length; argumentIndex++)
                {
                    uint variationIndex = 0;
                    bool invalidIndex = false;

                    try
                    {
                        variationIndex = uint.Parse(args[argumentIndex]);
                    }
                    catch (FormatException)
                    {
                        invalidIndex = true;
                    }

                    if (!invalidIndex && variationIndex >= variationMask.Length)
                    {
                        invalidIndex = true;
                    }

                    if (invalidIndex)
                    {
                        Console.WriteLine("{0} is not a valid integer test variation", args[argumentIndex]);

                        PrintUsage(lockTypes, testSpecifications);

                        return -1;
                    }

                    variationMask[variationIndex] = true;
                    variationsIncluded++;
                }
            }

            List<Thread> testThreads = new List<Thread>();
            Dictionary<Thread, ILockTest> threadToTestMap = new Dictionary<Thread, ILockTest>();

            int currentVariationIndex = 0;

            foreach (Type lockType in lockTypes)
            {
                ISyncLock syncLock = (ISyncLock)CreateTypeInstance(lockType);

                foreach (TestSpecification testSpecification in testSpecifications)
                {
                    int thisVariationIndex = currentVariationIndex++;

                    if (variationMask != null && !variationMask[thisVariationIndex])
                    {
                        continue;
                    }

                    ILockTest test = CreateTest(testSpecification.TestType, testSpecification.Parameters);

                    Console.WriteLine("Testing: {3}: {0} - Variation: {1}: {2}", (syncLock == null) ? "No lock" : syncLock.ToString(), test, test.Description, thisVariationIndex);

                    test.Initialize();

                    currentTest = test;

                    Thread testThread = new Thread(TestThread);

                    testThreads.Add(testThread);
                    threadToTestMap.Add(testThread, test);

                    testThread.Start(syncLock);

                    int testWaitTime = testParameters.MaxWaitSeconds * 2;

                    bool completedInTime = testThread.Join(testWaitTime * 1000);

                    if (!completedInTime)
                    {
                        if (null == lastTestException)
                        {
                            lastTestException = new TimeoutException(string.Format("Variation failed to complete within {0} seconds", testWaitTime));
                        }
                    }

                    if (lastTestException != null)
                    {
                        Console.Error.WriteLine("Error: {0}: {1}", lastTestException.GetType(), lastTestException.Message);
                        failedTests++;
                        Console.WriteLine("FAILED: {0} - Variation: {1}", syncLock == null ? "No lock" : syncLock.ToString(), currentTest);
                        continue;
                    }

                    passedTests++;
                    Console.WriteLine("PASSED: {0} - Variation: {1}", syncLock, test);
                }
            }

            Console.WriteLine("\t{0} passed, {1} failed, {2} skipped, {3} total", passedTests, failedTests, lockTypes.Length * testSpecifications.Length - variationsIncluded, lockTypes.Length * testSpecifications.Length);

            if (failedTests == 0)
            {
                Console.WriteLine("Success -- all variations passed.");
            }
            else
            {
                Console.WriteLine("Failure -- one or more variations failed");
            }

            foreach (Thread testThread in testThreads)
            {
                Console.WriteLine("Thread id: {0}, State: {1}", testThread.ManagedThreadId, testThread.ThreadState.ToString());

                if (testThread.ThreadState == ThreadState.Running || testThread.ThreadState == ThreadState.WaitSleepJoin)
                {
                    ILockTest runningTest = threadToTestMap[testThread];

                    Console.WriteLine("Unfinished test will be forcibly terminated: {0}: {1}", runningTest.GetType().ToString(), runningTest.Description);

                    testThread.Abort();
                }  
            }

            int exitCode = ( failedTests == 0 ) ? 0 : 1;
 
            Console.WriteLine("Exiting test with {0}\n", exitCode);

            return exitCode;
        }

        static void TestThread(object context)
        {
            lastTestException = null;

            ISyncLock syncLock = (ISyncLock)context;

            try
            {
                currentTest.RunTest(syncLock, testParameters);
            }
            catch (InapplicableTestException e)
            {
                Console.WriteLine("Skipping test: {0}", e.Message);
            }
            catch (Exception e)
            {
                lastTestException = e;
            }
        }

        static ILockTest CreateTest( Type testType, Type lockType, object[] parameters )
        {
            if ( ! testType.IsSubclassOf(typeof(ILockTest)) )
            {
                throw new InvalidCastException("Test type must be derived from ILockTest");
            }

            if ( null != lockType )
            {
                if ( ! testType.IsSubclassOf(typeof(ISyncLock)) )
                {
                    throw new InvalidCastException("Lock type must be derived from ISyncLock");
                }
            }

            ISyncLock syncLock = (ISyncLock) CreateTypeInstance(lockType);

            if ( null == syncLock )
            {
                throw new ArgumentException("Unable to construct an ISyncLock instance with specified parameters");
            }

            ILockTest test = (ILockTest) CreateTypeInstance(testType, new object[] {syncLock});

            if ( null == test )
            {
                throw new ArgumentException("Unable to construct an ILockTest instance with specified ISyncLock");
            }

            return test;
        }

        static ILockTest CreateTest(Type testType, object[] parameters)
        {
            if (testType.GetInterface(typeof(ILockTest).ToString()) == null)
            {
                throw new InvalidCastException("Test type must implement ILockTest");
            }
            
            ILockTest test = (ILockTest)CreateTypeInstance(testType, parameters);

            if (null == test)
            {
                throw new ArgumentException("Unable to construct an ILockTest instance with specified parameters");
            }

            return test;
        }

        static object CreateTypeInstance( Type type, object[] constructorParameters = null )
        {
            if (type == null)
            {
                return null;
            }

            object result = null;

            Type[] parameterTypes = new Type[constructorParameters != null ? constructorParameters.Length : 0];

            if (constructorParameters != null)
            {
                int typeIndex = 0;

                foreach (object parameter in constructorParameters)
                {
                    parameterTypes[typeIndex++] = parameter != null ? parameter.GetType() : null;
                }
            }
            
            ConstructorInfo constructorInfo = type.GetConstructor(parameterTypes);                

            if (null != constructorInfo)
            {
                result = constructorInfo.Invoke(constructorParameters);
            }            

            return result;
        }

        public class TestSpecification
        {
            public TestSpecification(Type testType, params object[] parameters)
            {
                TestType = testType;
                Parameters = parameters;
            }

            public Type TestType { get; private set; }

            public object[] Parameters{ get; private set;}
        }

        static void PrintUsage(Type[] lockTypes, TestSpecification[] testSpecifications)
        {
            Console.WriteLine("\nLocktest -t [<test-timeout>] [<test-variation> [<test-variation> [<test-variation>]...]]");
            Console.WriteLine("\nWhere <test-variation> is an integer from the list below indicating lock / test");
            Console.WriteLine("combination to execute\n");

            int testIndex = 0;

            foreach (Type lockType in lockTypes)
            {
                foreach (TestSpecification testSpecification in testSpecifications)
                {
                    ILockTest test = CreateTest(testSpecification.TestType, testSpecification.Parameters);

                    Console.Write("\t{0} ", testIndex);
                    PrintTest(lockType, test);
                    testIndex++;
                }
            }
        }

        static void PrintTest(Type lockType, ILockTest lockTest)
        {
            string lockName = lockType == null ? "No lock" : lockType.ToString();

            Console.WriteLine("Lock: '{0}' - {1}\n\t\tDescription: {2}", lockName, lockTest.GetType().ToString(), lockTest.Description);
        }
        
        static int passedTests;
        static int failedTests;

        static TestParameters testParameters;

        static Exception lastTestException;

        static ILockTest currentTest;
    }
}