using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace QfPlayAgent.Samples.TableNine
{
    public sealed class TableNinePlayAgentArchitectureProvider : IPlayAgentArchitectureProvider
    {
        public string ArchitectureId => "TableNine";

        public bool IsReady
        {
            get
            {
                if (global::TableNine.IsInitialized)
                {
                    return true;
                }

                if (!Application.isPlaying)
                {
                    global::TableNine.InitArchitecture();
                }

                return global::TableNine.IsInitialized;
            }
        }

        public IArchitecture GetArchitecture()
        {
            return global::TableNine.Interface;
        }
    }
}
