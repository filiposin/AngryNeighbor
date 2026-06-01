#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

// ReSharper disable UnusedMember.Global
// ReSharper disable once IdentifierTypo

namespace Neuston.UnusedAssetFinder
{
	public interface IUnusedAssetConfiguration
	{
		public void FilterAssetPaths(List<string> assetPaths);
	}

	class UnusedAssetConfigurationSingleton
	{
		static IUnusedAssetConfiguration? instance;

		public static IUnusedAssetConfiguration Instance
		{
			get
			{
				if (instance != null)
				{
					return instance;
				}

				var types = TypeCache.GetTypesDerivedFrom<IUnusedAssetConfiguration>()
					.Where(type => type != typeof(DefaultUnusedAssetConfiguration) && !type.IsAbstract && !type.IsInterface)
					.ToList();

				if (types.Count == 0)
				{
					instance = new DefaultUnusedAssetConfiguration();
					return instance;
				}

				if (types.Count > 1)
				{
					throw new UnusedAssetConfigurationException("More than one IUnusedAssetConfiguration found. Create only one concrete implementation of IUnusedAssetConfiguration in your project.");
				}

				var type = types[0];

				instance = (IUnusedAssetConfiguration)Activator.CreateInstance(type);

				return instance;
			}
		}
	}

	class DefaultUnusedAssetConfiguration : IUnusedAssetConfiguration
	{
		public void FilterAssetPaths(List<string> assetPaths)
		{
		}
	}

	public class UnusedAssetConfigurationException : Exception
	{
		public UnusedAssetConfigurationException()
		{
		}

		public UnusedAssetConfigurationException(string message) : base(message)
		{
		}

		public UnusedAssetConfigurationException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}
