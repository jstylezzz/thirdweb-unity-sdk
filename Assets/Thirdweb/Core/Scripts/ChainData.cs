using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Thirdweb
{
	[Serializable]
	public class ChainData
	{
		#region Properties

		public string Identifier => _identifier;

		public string ChainId => _chainId;

		public string RPCOverride => _rpcOverride;

		#endregion

		#region Editor Variables

		[SerializeField, FormerlySerializedAs("identifier")]
		private string _identifier;

		[SerializeField, FormerlySerializedAs("chainId")]
		private string _chainId;

		[SerializeField, FormerlySerializedAs("rpcOverride")]
		private string _rpcOverride;

		#endregion

		public ChainData(string identifier, string chainId, string rpcOverride)
		{
			_identifier = identifier;
			_chainId = chainId;
			_rpcOverride = rpcOverride;
		}
	}
}