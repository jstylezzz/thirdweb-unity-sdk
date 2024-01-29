using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using System.Numerics;
using Thirdweb;
using Thirdweb.Redcode.Awaiting;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Prefab_ConnectWallet : MonoBehaviour
{
	#region Editor Variables

	[FormerlySerializedAs("enabledWalletProviders"),Header("Enabled Wallet Providers. Press Play to see changes."), SerializeField]
	private List<WalletProvider> _enabledWalletProviders = new List<WalletProvider>
	{
		WalletProvider.LocalWallet,
		WalletProvider.EmbeddedWallet,
		WalletProvider.SmartWallet
	};

	[FormerlySerializedAs("useSmartWallets"),Header("Use ERC-4337 (Account Abstraction) compatible smart wallets.\nEnabling this will connect user to the associated smart wallet as per your ThirwebManager settings."), SerializeField]
	private bool _useSmartWallets;

	[FormerlySerializedAs("onStart"),Header("Events"), SerializeField]
	private UnityEvent _onStart;

	[FormerlySerializedAs("onConnectionRequested"),SerializeField]
	private UnityEvent<WalletConnection> _onConnectionRequested;
	
	[FormerlySerializedAs("onConnected"),SerializeField]
	private UnityEvent<string> _onConnected;
	
	[FormerlySerializedAs("onConnectionError"),SerializeField]
	private UnityEvent<Exception> _onConnectionError;
	
	[FormerlySerializedAs("onDisconnected"),SerializeField]
	private UnityEvent _onDisconnected;
	
	[FormerlySerializedAs("onSwitchNetwork"),SerializeField]
	private UnityEvent _onSwitchNetwork;

	[FormerlySerializedAs("walletProviderUI"),Header("UI"), SerializeField]
	private WalletProviderUIDictionary _walletProviderUI;

	[FormerlySerializedAs("emailInput"),SerializeField]
	private TMP_InputField _emailInput;
	
	[FormerlySerializedAs("exportButton"),SerializeField]
	private GameObject _exportButton;
	
	[FormerlySerializedAs("walletImages"),SerializeField]
	private List<Image> _walletImages;
	
	[FormerlySerializedAs("addressTexts"),SerializeField]
	private List<TMP_Text> _addressTexts;
	
	[FormerlySerializedAs("balanceTexts"),SerializeField]
	private List<TMP_Text> _balanceTexts;
	
	[FormerlySerializedAs("networkSwitchButton"),SerializeField]
	private Button _networkSwitchButton;
	
	[FormerlySerializedAs("switchNetworkContent"),SerializeField]
	private Transform _switchNetworkContent;
	
	[FormerlySerializedAs("switchNetworkButtonPrefab"),SerializeField]
	private GameObject _switchNetworkButtonPrefab;
	
	[FormerlySerializedAs("networkIcons"),SerializeField]
	private List<NetworkIcon> _networkIcons;
	
	[FormerlySerializedAs("walletIcons"),SerializeField]
	private List<WalletIcon> _walletIcons;
	
	[FormerlySerializedAs("currentNetworkIcon"),SerializeField]
	private Image _currentNetworkIcon;
	
	[FormerlySerializedAs("currentNetworkText"),SerializeField]
	private TMP_Text _currentNetworkText;

	#endregion

	#region Variables
    
	private string _address;
	private string _password;
	private ChainData _currentChainData;
    
	#endregion

	#region Lifecycle

	private void Start()
	{
		_address = null;
		_password = null;

		_currentChainData = ThirdwebManager.Instance.GetSupportedChainDataById(ThirdwebManager.Instance.ActiveChain);

		_networkSwitchButton.interactable = ThirdwebManager.Instance.SupportedChains.Count > 1;

		foreach(KeyValuePair<WalletProvider, GameObject> walletProvider in _walletProviderUI)
		{
			walletProvider.Value.SetActive(_enabledWalletProviders.Contains(walletProvider.Key));
		}

		_onStart.Invoke();
	}

	#endregion

	#region Public Methods

	public async void ExportWallet()
	{
		ThirdwebDebug.Log("Exporting wallet...");
		string json = await ThirdwebManager.Instance.SDK.wallet.Export(_password);
		GUIUtility.systemCopyBuffer = json;
		ThirdwebDebug.Log($"Copied wallet to clipboard: {json}");
	}

	public void CopyAddress()
	{
		GUIUtility.systemCopyBuffer = _address;
		ThirdwebDebug.Log($"Copied address to clipboard: {_address}");
	}

	public void ToggleSwitchNetworkPanel()
	{
		foreach(Transform item in _switchNetworkContent)
		{
			Destroy(item.gameObject);
		}

		foreach(ChainData chain in ThirdwebManager.Instance.SupportedChains)
		{
			if(chain.Identifier == _currentChainData.Identifier)
			{
				continue;
			}

			ChainData chainData = ThirdwebManager.Instance.GetSupportedChainDataById(chain.Identifier);
			GameObject chainButton = Instantiate(_switchNetworkButtonPrefab, _switchNetworkContent);
			Transform chainButtonImage = chainButton.transform.Find("Image_Network");
			Transform chainButtonText = chainButton.transform.Find("Text_Network");
			chainButtonText.GetComponentInChildren<TMP_Text>().text = PrettifyNetwork(chain.Identifier);
			chainButton.GetComponent<Button>().onClick.RemoveAllListeners();
			chainButton.GetComponent<Button>().onClick.AddListener(() => SwitchNetworkAsync(chainData));

			Sprite spriteToUse = null;
			NetworkIcon foundNetworkIcon = _networkIcons.Find(x => x.chain == chain.Identifier);
			if(foundNetworkIcon.sprite != null)
			{
				spriteToUse = foundNetworkIcon.sprite;
			}
			else if(_networkIcons.Count > 0 && _networkIcons[0].sprite != null)
			{
				spriteToUse = _networkIcons[0].sprite;
			}

			if(spriteToUse != null)
			{
				chainButtonImage.GetComponentInChildren<Image>().sprite = spriteToUse;
			}
		}
	}

	public void ConnectGuest(string password)
	{
		_password = password;
		WalletConnection wc = _useSmartWallets ? new WalletConnection(provider : WalletProvider.SmartWallet, chainId : BigInteger.Parse(_currentChainData.ChainId), password : _password, personalWallet : WalletProvider.LocalWallet) : new WalletConnection(provider : WalletProvider.LocalWallet, chainId : BigInteger.Parse(_currentChainData.ChainId), password : _password);
		ConnectAsync(wc);
	}

	public void ConnectOauth(string authProviderStr)
	{
		WalletConnection wc = _useSmartWallets ? new WalletConnection(provider : WalletProvider.SmartWallet, chainId : BigInteger.Parse(_currentChainData.ChainId), authOptions : new AuthOptions(Enum.Parse<AuthProvider>(authProviderStr)), personalWallet : WalletProvider.EmbeddedWallet) : new WalletConnection(provider : WalletProvider.EmbeddedWallet, chainId : BigInteger.Parse(_currentChainData.ChainId), authOptions : new AuthOptions(Enum.Parse<AuthProvider>(authProviderStr)));
		ConnectAsync(wc);
	}

	public void ConnectEmail()
	{
		WalletConnection wc = _useSmartWallets ? new WalletConnection(provider : WalletProvider.SmartWallet, chainId : BigInteger.Parse(_currentChainData.ChainId), email : _emailInput.text, authOptions : new AuthOptions(AuthProvider.EmailOTP), personalWallet : WalletProvider.EmbeddedWallet) : new WalletConnection(provider : WalletProvider.EmbeddedWallet, chainId : BigInteger.Parse(_currentChainData.ChainId), email : _emailInput.text, authOptions : new AuthOptions(AuthProvider.EmailOTP));
		ConnectAsync(wc);
	}

	public void ConnectExternal(string walletProviderStr)
	{
		WalletConnection wc = _useSmartWallets ? new WalletConnection(provider : WalletProvider.SmartWallet, chainId : BigInteger.Parse(_currentChainData.ChainId), personalWallet : Enum.Parse<WalletProvider>(walletProviderStr)) : new WalletConnection(provider : Enum.Parse<WalletProvider>(walletProviderStr), chainId : BigInteger.Parse(_currentChainData.ChainId));
		ConnectAsync(wc);
	}

	public async void Disconnect()
	{
		ThirdwebDebug.Log("Disconnecting...");
		try
		{
			_address = null;
			_password = null;
			await ThirdwebManager.Instance.SDK.wallet.Disconnect();
			_onDisconnected.Invoke();
		}
		catch(Exception e)
		{
			ThirdwebDebug.LogError($"Failed to disconnect: {e}");
		}
	}

	#endregion

	#region Private Methods

	private string PrettifyNetwork(string networkIdentifier)
	{
		string replaced = networkIdentifier.Replace("-", " ");
		return replaced.Substring(0, 1).ToUpper() + replaced.Substring(1);
	}

	private async void SwitchNetworkAsync(ChainData chainData)
	{
		ThirdwebDebug.Log($"Switching to network: {chainData.Identifier}...");
		try
		{
			await ThirdwebManager.Instance.SDK.wallet.SwitchNetwork(BigInteger.Parse(chainData.ChainId));
			ThirdwebManager.Instance.SwitchActiveChain(chainData.Identifier);
			_currentChainData = ThirdwebManager.Instance.GetSupportedChainDataById(ThirdwebManager.Instance.ActiveChain);

			ThirdwebDebug.Log($"Switched to network: {chainData.Identifier}");

			_onSwitchNetwork?.Invoke();
			PostConnectAsync();
		}
		catch(Exception e)
		{
			ThirdwebDebug.LogWarning($"Could not switch network! {e}");
		}
	}

	private Sprite GetWalletIconForProvider(WalletProvider provider)
	{
		WalletIcon wIcon = _walletIcons.Find(x => x.provider == provider);
		if(wIcon.sprite != null)
		{
			return wIcon.sprite;
		}

		if(_walletIcons.Count > 0 && _walletIcons[0].sprite != null)
		{
			return _walletIcons[0].sprite;
		}

		return null;
	}

	private Sprite GetNetworkIconForChainId(string chainIdentifier)
	{
		NetworkIcon nIcon = _networkIcons.Find(x => x.chain == chainIdentifier);
		if(nIcon.sprite != null)
		{
			return nIcon.sprite;
		}

		if(_networkIcons.Count > 0 && _networkIcons[0].sprite != null)
		{
			return _walletIcons[0].sprite;
		}

		return null;
	}

	private async void ConnectAsync(WalletConnection wc)
	{
		ThirdwebDebug.Log($"Connecting to {wc.provider}...");

		_onConnectionRequested.Invoke(wc);

		await new WaitForSeconds(0.5f);

		try
		{
			_address = await ThirdwebManager.Instance.SDK.wallet.Connect(wc);
			_exportButton.SetActive(wc.provider == WalletProvider.LocalWallet);
		}
		catch(Exception e)
		{
			_address = null;
			ThirdwebDebug.LogError($"Failed to connect: {e}");
			_onConnectionError.Invoke(e);
			return;
		}

		PostConnectAsync(wc);
	}

	private async void PostConnectAsync(WalletConnection wc = null)
	{
		ThirdwebDebug.Log($"Connected to {_address}");

		string addy = _address.ShortenAddress();
		foreach(TMP_Text addressText in _addressTexts)
		{
			addressText.text = addy;
		}

		CurrencyValue bal = await ThirdwebManager.Instance.SDK.wallet.GetBalance();
		string balStr = $"{bal.value.ToEth()} {bal.symbol}";
		foreach(TMP_Text balanceText in _balanceTexts)
		{
			balanceText.text = balStr;
		}

		if(wc != null)
		{
			Sprite currentWalletIcon = GetWalletIconForProvider(wc.provider);
			if(currentWalletIcon != null)
			{
				foreach(Image walletImage in _walletImages)
				{
					walletImage.sprite = currentWalletIcon;
				}
			}
		}

		Sprite networkIconSprite = GetNetworkIconForChainId(_currentChainData.Identifier);
		if(networkIconSprite != null)
		{
			_currentNetworkIcon.sprite = networkIconSprite;
		}

		_currentNetworkText.text = PrettifyNetwork(_currentChainData.Identifier);

		_onConnected.Invoke(_address);
	}

	#endregion
	
	#region Nested Classes

	[Serializable]
	public struct NetworkIcon
	{
		public string chain;
		public Sprite sprite;
	}

	[Serializable]
	public struct WalletIcon
	{
		public WalletProvider provider;
		public Sprite sprite;
	}
	
	[Serializable]
	public class WalletProviderUIDictionary : SerializableDictionaryBase<WalletProvider, GameObject>
	{
	}

	#endregion
}