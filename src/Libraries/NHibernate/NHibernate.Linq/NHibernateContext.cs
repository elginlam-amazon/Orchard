using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NHibernate.Metadata;

namespace NHibernate.Linq
{
	/// <summary>
	/// Wraps an <see cref="T:NHibernate.ISession"/> object to provide base functionality
	/// for custom, database-specific context classes.
	/// </summary>
	public abstract class NHibernateContext : IDisposable, ICloneable
	{
		/// <summary>
		/// Provides access to database provider specific methods.
		/// </summary>
		public readonly IDbMethods Methods;

		private ISession session;

		/// <summary>
		/// Initializes a new instance of the <see cref="NHibernateContext"/> class.
		/// </summary>
		public NHibernateContext()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NHibernate.Linq.NHibernateContext"/> class.
		/// </summary>
		/// <param name="session">An initialized <see cref="T:NHibernate.ISession"/> object.</param>
		public NHibernateContext(ISession session)
		{
			this.session = session;
		}

		/// <summary>
		/// Gets a reference to the <see cref="T:NHibernate.ISession"/> associated with this object.
		/// </summary>
		public virtual ISession Session
		{
			get
			{
				if (session == null)
				{
					// Attempt to get the Session
					session = ProvideSession();
				}
				return session;
			}
		}



		/// <summary>
		/// Allows for empty construction but provides an interface for an interface to have the derived 
		/// classes provide a session object late in the cycle. 
		/// </summary>
		/// <returns>The Required <see cref="T:NHibernate.ISession"/> object.</returns>
		protected virtual ISession ProvideSession()
		{
			// Should not be called as supplying the session in the constructor
			throw new NotImplementedException("If NHibernateContext is constructed with the empty constructor, inheritor is required to override ProvideSession to supply Session.");
		}

		#region ICloneable Members

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns></returns>
		public virtual object Clone()
		{
			if (session == null)
			{
				throw new ArgumentNullException("session");
			}

			return Activator.CreateInstance(GetType(), session);
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Disposes the wrapped <see cref="T:NHibernate.ISession"/> object.
		/// </summary>
		public virtual void Dispose()
		{
			if (session != null)
			{
				session.Dispose();
				session = null;
			}
		}

		#endregion

		// The WCF Data Services (System.Data.Services) IUpdatable / IExpandProvider implementations were removed:
		// System.Data.Services is not available on .NET (Core) and nothing in Orchard consumed them.
	}
}