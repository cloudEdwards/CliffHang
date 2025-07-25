namespace Fusion.Addons.KCC
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	// This file contains implementation related to interactions - modifiers, collisions and processors.
	public partial class KCC
	{
		// PUBLIC METHODS

		/// <summary>
		/// Check if the <c>KCC</c> has a collision with interaction provider of type T in <c>KCCData.Collisions</c>.
		/// </summary>
		public bool HasCollision<T>() where T : class
		{
			return Data.Collisions.HasProvider<T>() == true;
		}

		/// <summary>
		/// Check if the <c>KCC</c> has a collision with interaction provider in <c>KCCData.Collisions</c>.
		/// </summary>
		/// <param name="provider">IKCCInteractionProvider instance.</param>
		public bool HasCollision<T>(T provider) where T : Component, IKCCInteractionProvider
		{
			if (provider == null)
				return false;

			return Data.Collisions.HasProvider(provider) == true;
		}

		/// <summary>
		/// Explicitly remove a collision (interaction provider) from <c>KCCData.Collisions</c>. Removed processor won't execute any pending stage method.
		/// Changes done in render will vanish with next fixed update.
		/// </summary>
		/// <param name="provider">IKCCInteractionProvider instance.</param>
		/// <param name="forceRemove">Ignore result from <c>IKCCInteractionProvider.CanStopInteraction()</c> and force remove the collision. Be careful with this option.</param>
		public bool RemoveCollision<T>(T provider, bool forceRemove = false) where T : Component, IKCCInteractionProvider
		{
			if (provider == null)
				return false;

			KCCData data = Data;

			KCCCollision collision = data.Collisions.Find(provider);
			if (collision == null)
				return false;

			return RemoveCollision(data, collision, forceRemove);
		}

		/// <summary>
		/// Check if the <c>KCC</c> has registered custom modifier (interaction provider) of type T in <c>KCCData.Modifiers</c>.
		/// </summary>
		public bool HasModifier<T>() where T : class
		{
			return Data.Modifiers.HasProvider<T>() == true;
		}

		/// <summary>
		/// Check if the <c>KCC</c> has registered custom modifier (interaction provider) in <c>KCCData.Modifiers</c>.
		/// </summary>
		/// <param name="provider">IKCCInteractionProvider instance.</param>
		public bool HasModifier<T>(T provider) where T : Component, IKCCInteractionProvider
		{
			if (provider == null)
				return false;

			return Data.Modifiers.HasProvider(provider) == true;
		}

		/// <summary>
		/// Returns any registered custom modifier (interaction provider) of type T from <c>KCCData.Modifiers</c>.
		/// </summary>
		public T GetModifier<T>() where T : class
		{
			return Data.Modifiers.GetProvider<T>();
		}

		/// <summary>
		/// Returns all registered custom modifiers (interaction providers) of type T from <c>KCCData.Modifiers</c>.
		/// </summary>
		/// <param name="providers">List filled with interaction providers of type T.</param>
		public void GetModifiers<T>(List<T> providers) where T : class
		{
			Data.Modifiers.GetProviders(providers, true);
		}

		/// <summary>
		/// Returns all registered custom modifiers (interaction providers) of type T from <c>KCCData.Modifiers</c>.
		/// </summary>
		public List<T> GetModifiers<T>() where T : class
		{
			List<T> providers = new List<T>();
			GetModifiers(providers);
			return providers;
		}

		/// <summary>
		/// Register custom modifier (interaction provider) to <c>KCCData.Modifiers</c>.
		/// Changes done in render will vanish with next fixed update.
		/// </summary>
		/// <param name="provider">IKCCInteractionProvider instance.</param>
		/// <param name="forceAdd">Ignore result from <c>IKCCInteractionProvider.CanStartInteraction()</c> and force add the modifier. Be careful with this option.</param>
		public bool AddModifier<T>(T provider, bool forceAdd = false) where T : Component, IKCCInteractionProvider
		{
			if (provider == null)
				return false;
			if (CheckSpawned() == false)
				return false;

			KCCData data = Data;

			if (data.Modifiers.HasProvider(provider) == true)
				return false;

			NetworkObject networkObject = provider.GetComponentNoAlloc<NetworkObject>();
			if (networkObject == null)
			{
				LogError($"Interaction provider {provider.name} doesn't have {nameof(NetworkObject)} component! Ignoring.", provider.gameObject);
				return false;
			}

			IKCCInteractionProvider checkProvider = provider.gameObject.GetComponentNoAlloc<IKCCInteractionProvider>();
			if (object.ReferenceEquals(checkProvider, provider) == false)
			{
				LogError($"Object {provider.name} has multiple {nameof(IKCCInteractionProvider)} components, this is not allowed for custom modifiers! Ignoring.", provider.gameObject);
				return false;
			}

			if (forceAdd == false && provider.CanStartInteraction(this, data) == false)
				return false;

			KCCModifier modifier = data.Modifiers.Add(networkObject, provider);
			if (modifier.Processor != null)
			{
				OnProcessorAdded(data, modifier.Processor);
			}

			return true;
		}

		/// <summary>
		/// Try to register custom modifier (interaction provider) to <c>KCCData.Modifiers</c>.
		/// Changes done in render will vanish with next fixed update.
		/// </summary>
		/// <param name="provider">IKCCInteractionProvider instance.</param>
		/// <param name="forceAdd">Ignore result from <c>IKCCInteractionProvider.CanStartInteraction()</c> and force add the modifier. Be careful with this option.</param>
		public bool TryAddModifier(IKCCInteractionProvider provider, bool forceAdd = false)
		{
			if (provider == null)
				return false;
			if (CheckSpawned() == false)
				return false;

			Component providerComponent = provider as Component;
			if (object.ReferenceEquals(providerComponent, null) == true)
				return false;

			KCCData data = Data;

			if (data.Modifiers.HasProvider(provider) == true)
				return false;

			NetworkObject networkObject = providerComponent.GetComponentNoAlloc<NetworkObject>();
			if (networkObject == null)
			{
				LogError($"Interaction provider {providerComponent.name} doesn't have {nameof(NetworkObject)} component! Ignoring.", providerComponent.gameObject);
				return false;
			}

			IKCCInteractionProvider checkProvider = providerComponent.gameObject.GetComponentNoAlloc<IKCCInteractionProvider>();
			if (object.ReferenceEquals(checkProvider, provider) == false)
			{
				LogError($"Object {providerComponent.name} has multiple {nameof(IKCCInteractionProvider)} components, this is not allowed for custom modifiers! Ignoring.", providerComponent.gameObject);
				return false;
			}

			if (forceAdd == false && provider.CanStartInteraction(this, data) == false)
				return false;

			KCCModifier modifier = data.Modifiers.Add(networkObject, provider);
			if (modifier.Processor != null)
			{
				OnProcessorAdded(data, modifier.Processor);
			}

			return true;
		}

		/// <summary>
		/// Try to register custom modifier (interaction provider) to <c>KCCData.Modifiers</c> if the network object has any.
		/// Changes done in render will vanish with next fixed update.
		/// </summary>
		/// <param name="networkObject">NetworkObject of the modifier.</param>
		/// <param name="forceAdd">Ignore result from <c>IKCCInteractionProvider.CanStartInteraction()</c> and force add the modifier. Be careful with this option.</param>
		public bool TryAddModifier(NetworkObject networkObject, bool forceAdd = false)
		{
			if (networkObject == null)
				return false;
			if (CheckSpawned() == false)
				return false;

			IKCCInteractionProvider provider = networkObject.GetComponentNoAlloc<IKCCInteractionProvider>();
			if (provider == null)
				return false;

			KCCData data = Data;

			if (data.Modifiers.HasProvider(provider) == true)
				return false;

			if (forceAdd == false && provider.CanStartInteraction(this, data) == false)
				return false;

			KCCModifier modifier = data.Modifiers.Add(networkObject, provider);
			if (modifier.Processor != null)
			{
				OnProcessorAdded(data, modifier.Processor);
			}

			return true;
		}

		/// <summary>
		/// Unregister custom modifier (interaction provider) from <c>KCCData.Modifiers</c>. Removed processor won't execute any pending stage method.
		/// Changes done in render will vanish with next fixed update.
		/// </summary>
		/// <param name="provider">IKCCInteractionProvider instance.</param>
		/// <param name="forceRemove">Ignore result from <c>IKCCInteractionProvider.CanStopInteraction()</c> and force remove the modifier. Be careful with this option.</param>
		public bool RemoveModifier<T>(T provider, bool forceRemove = false) where T : Component, IKCCInteractionProvider
		{
			if (provider == null)
				return false;

			KCCData data = Data;

			KCCModifier modifier = data.Modifiers.Find(provider);
			if (modifier == null)
				return false;

			return RemoveModifier(data, modifier, forceRemove);
		}

		/// <summary>
		/// Check if the <c>KCC</c> has registered any interaction provider of type T.
		/// </summary>
		public bool HasInteraction<T>() where T : class
		{
			KCCData data = Data;

			if (data.Modifiers.HasProvider<T>() == true)
				return true;
			if (data.Collisions.HasProvider<T>() == true)
				return true;

			return false;
		}

		/// <summary>
		/// Check if the <c>KCC</c> has registered interaction provider.
		/// </summary>
		/// <param name="provider">IKCCInteractionProvider instance.</param>
		public bool HasInteraction<T>(T provider) where T : Component, IKCCInteractionProvider
		{
			if (provider == null)
				return false;

			KCCData data = Data;

			if (data.Modifiers.HasProvider(provider) == true)
				return true;
			if (data.Collisions.HasProvider(provider) == true)
				return true;

			return false;
		}

		/// <summary>
		/// Returns any registered interaction provider of type T.
		/// </summary>
		public T GetInteraction<T>() where T : class
		{
			T provider;

			KCCData data = Data;

			provider = data.Modifiers.GetProvider<T>();
			if (object.ReferenceEquals(provider, null) == false)
				return provider;

			provider = data.Collisions.GetProvider<T>();
			if (object.ReferenceEquals(provider, null) == false)
				return provider;

			return null;
		}

		/// <summary>
		/// Returns all registered interaction providers of type T.
		/// </summary>
		/// <param name="providers">List filled with interaction providers of type T.</param>
		public void GetInteractions<T>(List<T> providers) where T : class
		{
			providers.Clear();

			KCCData data = Data;

			data.Modifiers.GetProviders(providers, false);
			data.Collisions.GetProviders(providers, false);
		}

		/// <summary>
		/// Returns all registered interaction providers of type T.
		/// </summary>
		public List<T> GetInteractions<T>() where T : class
		{
			List<T> providers = new List<T>();
			GetInteractions(providers);
			return providers;
		}

		/// <summary>
		/// Unregister custom modifier (interaction provider) from <c>KCCData.Modifiers</c> or remove a collision from <c>KCCData.Collisions</c>. Removed processor won't execute any pending stage method.
		/// Changes done in render will vanish with next fixed update.
		/// </summary>
		/// <param name="provider">Interaction provider instance.</param>
		/// <param name="forceRemove">Ignore result from <c>IKCCInteractionProvider.CanStopInteraction()</c> and force remove the interaction. Be careful with this option.</param>
		public bool RemoveInteraction<T>(T provider, bool forceRemove = false) where T : Component, IKCCInteractionProvider
		{
			if (provider == null)
				return false;

			bool removed = false;

			removed |= RemoveModifier(provider, forceRemove);
			removed |= RemoveCollision(provider, forceRemove);

			return removed;
		}

		/// <summary>
		/// Check if the KCC interacts with any processor of type T.
		/// This method looks in modifiers, collisions, local and external processors.
		/// </summary>
		public bool HasProcessor<T>() where T : class
		{
			KCCData data = Data;

			if (data.Modifiers.HasProcessor<T>() == true)
				return true;
			if (data.Collisions.HasProcessor<T>() == true)
				return true;

			List<IKCCProcessor> localProcessors = _localProcessors;
			for (int i = 0, count = localProcessors.Count; i < count; ++i)
			{
				if (localProcessors[i] is T)
					return true;
			}

			if (GetExternalProcessors != null)
			{
				try
				{
					IList<IKCCProcessor> externalProcessors = GetExternalProcessors();
					if (externalProcessors != null && externalProcessors.Count > 0)
					{
						for (int i = 0, count = externalProcessors.Count; i < count; ++i)
						{
							if (externalProcessors[i] is T)
								return true;
						}
					}
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}

			return false;
		}

		/// <summary>
		/// Check if the KCC interacts with a given processor.
		/// This method looks in modifiers, collisions, local and external processors.
		/// </summary>
		/// <param name="processor">IKCCProcessor instance.</param>
		public bool HasProcessor<T>(T processor) where T : Component, IKCCProcessor
		{
			if (processor == null)
				return false;

			KCCData data = Data;

			if (data.Modifiers.HasProcessor(processor) == true)
				return true;
			if (data.Collisions.HasProcessor(processor) == true)
				return true;

			List<IKCCProcessor> localProcessors = _localProcessors;
			for (int i = 0, count = localProcessors.Count; i < count; ++i)
			{
				if (object.ReferenceEquals(localProcessors[i], processor) == true)
					return true;
			}

			if (GetExternalProcessors != null)
			{
				try
				{
					IList<IKCCProcessor> externalProcessors = GetExternalProcessors();
					if (externalProcessors != null && externalProcessors.Count > 0)
					{
						for (int i = 0, count = externalProcessors.Count; i < count; ++i)
						{
							if (object.ReferenceEquals(externalProcessors[i], processor) == true)
								return true;
						}
					}
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}

			return false;
		}

		/// <summary>
		/// Returns any interacting processor of type T.
		/// This method looks in modifiers, collisions, local and external processors.
		/// </summary>
		public T GetProcessor<T>() where T : class
		{
			T processor;

			KCCData data = Data;

			processor = data.Modifiers.GetProcessor<T>();
			if (object.ReferenceEquals(processor, null) == false)
				return processor;

			processor = data.Collisions.GetProcessor<T>();
			if (object.ReferenceEquals(processor, null) == false)
				return processor;

			List<IKCCProcessor> localProcessors = _localProcessors;
			for (int i = 0, count = localProcessors.Count; i < count; ++i)
			{
				if (localProcessors[i] is T localProcessor)
					return localProcessor;
			}

			if (GetExternalProcessors != null)
			{
				try
				{
					IList<IKCCProcessor> externalProcessors = GetExternalProcessors();
					if (externalProcessors != null && externalProcessors.Count > 0)
					{
						for (int i = 0, count = externalProcessors.Count; i < count; ++i)
						{
							if (externalProcessors[i] is T externalProcessor)
								return externalProcessor;
						}
					}
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}

			return null;
		}

		/// <summary>
		/// Returns all interacting processors of type T.
		/// This method looks in modifiers, collisions, local and external processors.
		/// </summary>
		/// <param name="processors">List filled with processors of type T.</param>
		/// <param name="sortByProcessorPriority">Processors are sorted by processor priority. For stage sorting use KCCUtility.SortStages().</param>
		public void GetProcessors<T>(List<T> processors, bool sortByProcessorPriority = false) where T : class
		{
			processors.Clear();

			KCCData data = Data;

			data.Modifiers.GetProcessors(processors, false);
			data.Collisions.GetProcessors(processors, false);

			List<IKCCProcessor> localProcessors = _localProcessors;
			for (int i = 0, count = localProcessors.Count; i < count; ++i)
			{
				if (localProcessors[i] is T localProcessor)
				{
					processors.Add(localProcessor);
				}
			}

			if (GetExternalProcessors != null)
			{
				try
				{
					IList<IKCCProcessor> externalProcessors = GetExternalProcessors();
					if (externalProcessors != null && externalProcessors.Count > 0)
					{
						for (int i = 0, count = externalProcessors.Count; i < count; ++i)
						{
							IKCCProcessor processor = externalProcessors[i];
							if (processor is T externalProcessor)
							{
								processors.Add(externalProcessor);
							}
						}
					}
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}

			if (sortByProcessorPriority == true)
			{
				KCCUtility.SortProcessors(this, processors);
			}
		}

		/// <summary>
		/// Returns all interacting processors of type T.
		/// This method looks in modifiers, collisions, local and external processors.
		/// </summary>
		/// <param name="sortByProcessorPriority">Processors are sorted by processor priority. For stage sorting use KCCUtility.SortStages().</param>
		public List<T> GetProcessors<T>(bool sortByProcessorPriority = false) where T : class
		{
			List<T> processors = new List<T>();
			GetProcessors(processors, sortByProcessorPriority);
			return processors;
		}

		/// <summary>
		/// Register local processor to <c>LocalProcessors</c> list. Local processors are NOT networked, be careful!
		/// Note: <c>KCCSettings.Processors</c> are added as local processors upon initialization.
		/// </summary>
		/// <param name="processor">IKCCProcessor instance.</param>
		public bool AddLocalProcessor(IKCCProcessor processor)
		{
			if (processor == null)
				throw new ArgumentNullException(nameof(processor));
			if (CheckSpawned() == false)
				return false;

			if (_localProcessors.Contains(processor) == true)
				return false;

			_localProcessors.Add(processor);

			try { processor.OnEnter(this, Data); } catch (Exception exception) { UnityEngine.Debug.LogException(exception); }

			return true;
		}

		/// <summary>
		/// Unregister local processor from <c>LocalProcessors</c> list. Local processors are NOT networked, be careful!
		/// </summary>
		/// <param name="processor">IKCCProcessor instance.</param>
		public bool RemoveLocalProcessor(IKCCProcessor processor)
		{
			if (processor == null)
				return false;

			if (_localProcessors.Remove(processor) == false)
				return false;

			try { processor.OnExit(this, Data); } catch (Exception exception) { UnityEngine.Debug.LogException(exception); }

			return true;
		}

		/// <summary>
		/// Add/remove networked collider to/from custom ignore list. The object must have <c>NetworkObject</c> component to correctly synchronize over network.
		/// Changes done in render will vanish with next fixed update.
		/// </summary>
		/// <returns><c>True</c> if there is a change in the ignore list.</returns>
		public bool SetIgnoreCollider(Collider ignoreCollider, bool ignore)
		{
			if (ignoreCollider == null)
				return false;
			if (CheckSpawned() == false)
				return false;

			KCCData data = Data;

			if (ignore == true)
			{
				if (data.Ignores.HasCollider(ignoreCollider) == true)
					return false;

				NetworkObject networkObject = ignoreCollider.GetComponentNoAlloc<NetworkObject>();
				if (networkObject == null)
				{
					LogError($"Collider {ignoreCollider.name} doesn't have {nameof(NetworkObject)} component! Ignoring.", ignoreCollider.gameObject);
					return false;
				}

				Collider checkCollider = ignoreCollider.gameObject.GetComponentNoAlloc<Collider>();
				if (object.ReferenceEquals(checkCollider, ignoreCollider) == false)
				{
					LogError($"Object {ignoreCollider.name} has multiple {nameof(Collider)} components, this is not allowed for ignored colliders! Ignoring.", ignoreCollider.gameObject);
					return false;
				}

				data.Ignores.Add(networkObject, ignoreCollider, false);
			}
			else
			{
				if (data.Ignores.Remove(ignoreCollider) == false)
					return false;
			}

			return true;
		}

		// PRIVATE METHODS

		private void UpdateCollisions(KCCData data, KCCOverlapInfo trackOverlapInfo)
		{
			int addCollisionsCount    = 0;
			int removeCollisionsCount = 0;

			List<KCCCollision> collisions = data.Collisions.All;
			for (int i = 0, count = collisions.Count; i < count; ++i)
			{
				KCCCollision collision = collisions[i];

				_removeColliders[removeCollisionsCount]  = collision.Collider;
				_removeCollisions[removeCollisionsCount] = collision;

				++removeCollisionsCount;
			}

			KCCOverlapHit[] trackHits = trackOverlapInfo.AllHits;
			for (int i = 0, count = trackOverlapInfo.AllHitCount; i < count; ++i)
			{
				Collider trackCollider      = trackHits[i].Collider;
				bool     trackColliderFound = false;

				for (int j = 0; j < removeCollisionsCount; ++j)
				{
					if (object.ReferenceEquals(_removeColliders[j], trackCollider) == true)
					{
						trackColliderFound = true;

						--removeCollisionsCount;

						_removeColliders[j]  = _removeColliders[removeCollisionsCount];
						_removeCollisions[j] = _removeCollisions[removeCollisionsCount];

						break;
					}
				}

				if (trackColliderFound == false)
				{
					_addColliders[addCollisionsCount] = trackCollider;
					++addCollisionsCount;
				}
			}

			for (int i = 0; i < removeCollisionsCount; ++i)
			{
				RemoveCollision(data, _removeCollisions[i], false);
			}

			for (int i = 0; i < addCollisionsCount; ++i)
			{
				AddCollision(data, _addColliders[i]);
			}
		}

		private bool AddCollision(KCCData data, Collider collisionCollider)
		{
			if (ReferenceEquals(collisionCollider, _lastNonNetworkedCollider) == true)
				return false;

			GameObject collisionObject = collisionCollider.gameObject;

			NetworkObject networkObject = collisionObject.GetComponentNoAlloc<NetworkObject>();
			if (networkObject == null)
			{
				_lastNonNetworkedCollider = collisionCollider;
				return false;
			}

			IKCCInteractionProvider interactionProvider = collisionObject.GetComponentNoAlloc<IKCCInteractionProvider>();
			if (interactionProvider != null && interactionProvider.CanStartInteraction(this, data) == false)
				return false;

			KCCCollision collision = data.Collisions.Add(networkObject, interactionProvider, collisionCollider);
			if (collision.Processor != null)
			{
				OnProcessorAdded(data, collision.Processor);
			}

			if (OnCollisionEnter != null)
			{
				try { OnCollisionEnter(this, collision); } catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
			}

			return true;
		}

		private bool RemoveCollision(KCCData data, KCCCollision collision, bool forceRemove)
		{
			if (forceRemove == false)
			{
				IKCCInteractionProvider interactionProvider = collision.Provider;
				if (interactionProvider != null)
				{
					try
					{
						if (interactionProvider.CanStopInteraction(this, data) == false)
							return false;
					}
					catch (Exception exception)
					{
						UnityEngine.Debug.LogException(exception);
					}
				}
			}

			if (OnCollisionExit != null)
			{
				try { OnCollisionExit(this, collision); } catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
			}

			if (collision.Processor != null)
			{
				OnProcessorRemoved(data, collision.Processor);
			}

			data.Collisions.Remove(collision);

			return true;
		}

		private void ForceRemoveAllCollisions(KCCData data)
		{
			List<KCCCollision> collisions = data.Collisions.All;
			while (collisions.Count > 0)
			{
				RemoveCollision(data, collisions[collisions.Count - 1], true);
			}
		}

		private bool RemoveModifier(KCCData data, KCCModifier modifier, bool forceRemove)
		{
			if (forceRemove == false)
			{
				IKCCInteractionProvider interactionProvider = modifier.Provider;
				if (interactionProvider != null)
				{
					try
					{
						if (interactionProvider.CanStopInteraction(this, data) == false)
							return false;
					}
					catch (Exception exception)
					{
						UnityEngine.Debug.LogException(exception);
					}
				}
			}

			IKCCProcessor processor = modifier.Processor;

			if (data.Modifiers.Remove(modifier) == true)
			{
				if (processor != null)
				{
					OnProcessorRemoved(data, processor);
				}
			}

			return true;
		}

		private void ForceRemoveAllModifiers(KCCData data)
		{
			List<KCCModifier> modifiers = data.Modifiers.All;
			while (modifiers.Count > 0)
			{
				RemoveModifier(data, modifiers[modifiers.Count - 1], true);
			}
		}

		private void OnProcessorAdded(KCCData data, IKCCProcessor processor)
		{
			try { processor.OnEnter(this, data); } catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
		}

		private void OnProcessorRemoved(KCCData data, IKCCProcessor processor)
		{
			for (int j = 0, stageCount = _activeStages.Count; j < stageCount; ++j)
			{
				KCCStageInfo    activeStageInfo       = _activeStages[j];
				IKCCProcessor[] activeStageProcessors = activeStageInfo.Processors;

				for (int i = 0, count = activeStageInfo.ProcessorCount; i < count; ++i)
				{
					if (ReferenceEquals(activeStageProcessors[i], processor) == true)
					{
						activeStageProcessors[i] = null;
						break;
					}
				}
			}

			if (_cachedProcessorCount > 0)
			{
				IKCCProcessor[] cachedProcessors = _cachedProcessors;
				for (int i = 0, count = _cachedProcessorCount; i < count; ++i)
				{
					if (ReferenceEquals(cachedProcessors[i], processor) == true)
					{
						cachedProcessors[i] = null;
						break;
					}
				}
			}

			if (_stageProcessorCount > 0)
			{
				IKCCProcessor[] stageProcessors = _stageProcessors;
				for (int i = 0, count = _stageProcessorCount; i < count; ++i)
				{
					if (ReferenceEquals(stageProcessors[i], processor) == true)
					{
						stageProcessors[i] = null;
						break;
					}
				}
			}

			try { processor.OnExit(this, data); } catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
		}

		private void CacheProcessors(KCCData data)
		{
			_cachedProcessorCount = 0;

			List<KCCModifier> modifiers = data.Modifiers.All;
			for (int i = 0, count = modifiers.Count; i < count; ++i)
			{
				KCCUtility.AddUniqueProcessor(this, modifiers[i].Processor, _cachedProcessors, ref _cachedProcessorCount);
			}

			List<KCCCollision> collisions = data.Collisions.All;
			for (int i = 0, count = collisions.Count; i < count; ++i)
			{
				KCCUtility.AddUniqueProcessor(this, collisions[i].Processor, _cachedProcessors, ref _cachedProcessorCount);
			}

			List<IKCCProcessor> localProcessors = _localProcessors;
			for (int i = 0, count = localProcessors.Count; i < count; ++i)
			{
				KCCUtility.AddUniqueProcessor(this, localProcessors[i], _cachedProcessors, ref _cachedProcessorCount);
			}

			if (GetExternalProcessors != null)
			{
				try
				{
					IList<IKCCProcessor> externalProcessors = GetExternalProcessors();
					if (externalProcessors != null && externalProcessors.Count > 0)
					{
						for (int i = 0, count = externalProcessors.Count; i < count; ++i)
						{
							KCCUtility.AddUniqueProcessor(this, externalProcessors[i], _cachedProcessors, ref _cachedProcessorCount);
						}
					}
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}

			KCCUtility.SortProcessors(this, _cachedProcessors, _cachedProcessorCount);

			_stageProcessorCount = _cachedProcessorCount;
			if (_stageProcessorCount > 0)
			{
				Array.Copy(_cachedProcessors, _stageProcessors, _stageProcessorCount);
			}
		}
	}
}
