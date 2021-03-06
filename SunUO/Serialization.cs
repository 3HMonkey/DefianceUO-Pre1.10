/***************************************************************************
 *                             Serialization.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *                          (C) 2006 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id: Serialization.cs 786 2006-12-24 12:08:23Z make $
 *   $Author: make $
 *   $Date: 2006-12-24 13:08:23 +0100 (Sun, 24 Dec 2006) $
 *
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Net;

using Server;
using Server.Guilds;

namespace Server
{
	public abstract class GenericReader
	{
		public GenericReader() { }

		public abstract string ReadString();
		public abstract DateTime ReadDateTime();
		public abstract TimeSpan ReadTimeSpan();
		public abstract DateTime ReadDeltaTime();
		public abstract decimal ReadDecimal();
		public abstract long ReadLong();
		public abstract ulong ReadULong();
		public abstract int ReadInt();
		public abstract uint ReadUInt();
		public abstract short ReadShort();
		public abstract ushort ReadUShort();
		public abstract double ReadDouble();
		public abstract float ReadFloat();
		public abstract char ReadChar();
		public abstract byte ReadByte();
		public abstract sbyte ReadSByte();
		public abstract bool ReadBool();
		public abstract int ReadEncodedInt();
		public abstract IPAddress ReadIPAddress();

		public abstract Point3D ReadPoint3D();
		public abstract Point2D ReadPoint2D();
		public abstract Rectangle2D ReadRect2D();
		public abstract Map ReadMap();

		public abstract Item ReadItem();
		public abstract Mobile ReadMobile();
		public abstract BaseGuild ReadGuild();

		public abstract ArrayList ReadItemList();
		public abstract ArrayList ReadMobileListOrNull();
		public abstract ArrayList ReadMobileList();
		public abstract ArrayList ReadGuildList();
		public abstract ArrayList ReadGuildListOrNull();

		public abstract bool End();
	}

	public abstract class GenericWriter
	{
		public GenericWriter() { }

		public abstract void Close();

		public abstract long Position { get; }

		public abstract void Write( string value );
		public abstract void Write( DateTime value );
		public abstract void Write( TimeSpan value );
		public abstract void Write( decimal value );
		public abstract void Write( long value );
		public abstract void Write( ulong value );
		public abstract void Write( int value );
		public abstract void Write( uint value );
		public abstract void Write( short value );
		public abstract void Write( ushort value);
		public abstract void Write( double value );
		public abstract void Write( float value );
		public abstract void Write( char value );
		public abstract void Write( byte value );
		public abstract void Write( sbyte value );
		public abstract void Write( bool value );
		public abstract void WriteEncodedInt( int value );
		public abstract void Write( IPAddress value );

		public abstract void WriteDeltaTime( DateTime value );

		public abstract void Write( Point3D value );
		public abstract void Write( Point2D value );
		public abstract void Write( Rectangle2D value );
		public abstract void Write( Map value );

		public abstract void Write( Item value );
		public abstract void Write( Mobile value );
		public abstract void Write( BaseGuild value );

		public abstract void WriteItemList( ArrayList list );
		public abstract void WriteItemList( ArrayList list, bool tidy );

		public abstract void WriteMobileList( ArrayList list );
		public abstract void WriteMobileList( ArrayList list, bool tidy );

		public abstract void WriteGuildList( ArrayList list );
		public abstract void WriteGuildList( ArrayList list, bool tidy );
	}

	public sealed class BinaryFileWriter : GenericWriter
	{
		private bool PrefixStrings;
		private FileStream m_File;

		private const int BufferSize = 32768;

		private byte[] m_Buffer;
		private int m_Index;

		private long m_Position = 0;

		private Encoding m_Encoding;

		public BinaryFileWriter( FileStream strm, bool prefixStr )
		{
			PrefixStrings = prefixStr;
			m_Encoding = Utility.UTF8;
			m_Buffer = new byte[BufferSize];
			m_File = strm;
		}

		public BinaryFileWriter( string filename, bool prefixStr )
		{
			PrefixStrings = prefixStr;
			m_Buffer = new byte[BufferSize];
			m_File = new FileStream( filename, FileMode.Create, FileAccess.Write, FileShare.None );
			m_Encoding = Utility.UTF8WithEncoding;
		}

		private void Flush()
		{
			if ( m_Index > 0 )
			{
				m_File.Write( m_Buffer, 0, m_Index );
				m_Position += m_Index;
				m_Index = 0;
			}
		}

		public override long Position
		{
			get
			{
				return m_Position + m_Index;
			}
		}

		public override void Close()
		{
			Flush();
			m_File.Close();
		}

		private void Write(byte[] src) {
			int length = src.Length;

			if (m_Index + length > BufferSize)
				Flush();

			for (int i = 0; i < length; i++)
				m_Buffer[m_Index + i] = src[i];

			m_Index += length;
		}

		public override void WriteEncodedInt( int value )
		{
			uint v = (uint) value;

			if ( (m_Index + 5) > BufferSize )
				Flush();

			while ( v >= 0x80 )
			{
				m_Buffer[m_Index++] = (byte) (v | 0x80);
				v >>= 7;
			}

			m_Buffer[m_Index++] = (byte) v;
		}

		private byte[] m_CharacterBuffer;
		private int m_MaxBufferChars;
		private const int LargeByteBufferSize = 256;

		internal void InternalWriteString( string value )
		{
			int length = m_Encoding.GetByteCount( value );

			WriteEncodedInt( length );

			if ( m_CharacterBuffer == null )
			{
				m_CharacterBuffer = new byte[LargeByteBufferSize];
				m_MaxBufferChars = LargeByteBufferSize / m_Encoding.GetMaxByteCount( 1 );
			}

			if ( length > LargeByteBufferSize )
			{
				int current = 0;
				int charsLeft = value.Length;

				while ( charsLeft > 0 )
				{
					int charCount = ( charsLeft > m_MaxBufferChars ) ? m_MaxBufferChars : charsLeft;
					int byteLength = m_Encoding.GetBytes( value, current, charCount, m_CharacterBuffer, 0 );

					if ( (m_Index + byteLength) > BufferSize )
						Flush();

					Buffer.BlockCopy( m_CharacterBuffer, 0, m_Buffer, m_Index, byteLength );
					m_Index += byteLength;

					current += charCount;
					charsLeft -= charCount;
				}
			}
			else
			{
				int byteLength = m_Encoding.GetBytes( value, 0, value.Length, m_CharacterBuffer, 0 );

				if ( (m_Index + byteLength) > BufferSize )
					Flush();

				Buffer.BlockCopy( m_CharacterBuffer, 0, m_Buffer, m_Index, byteLength );
				m_Index += byteLength;
			}
		}

		public override void Write( string value )
		{
			if ( PrefixStrings )
			{
				if ( value == null )
				{
					if ( (m_Index + 1) > BufferSize )
						Flush();

					m_Buffer[m_Index++] = 0;
				}
				else
				{
					if ( (m_Index + 1) > BufferSize )
						Flush();

					m_Buffer[m_Index++] = 1;

					InternalWriteString( value );
				}
			}
			else
			{
				InternalWriteString( value );
			}
		}

		public override void Write( DateTime value )
		{
			Write( value.Ticks );
		}

		public override void WriteDeltaTime( DateTime value )
		{
			long ticks = value.Ticks;
			long now = Core.Now.Ticks;

			TimeSpan d;

			try{ d = new TimeSpan( ticks-now ); }
			catch{ if ( ticks < now ) d = TimeSpan.MaxValue; else d = TimeSpan.MaxValue; }

			Write( d );
		}

		public override void Write( IPAddress value )
		{
			Write( value.Address );
		}

		public override void Write( TimeSpan value )
		{
			Write( value.Ticks );
		}

		public override void Write( decimal value )
		{
			int[] bits = Decimal.GetBits( value );

			for ( int i = 0; i < bits.Length; ++i )
				Write( bits[i] );
		}

		public override void Write( long value )
		{
			if ( (m_Index + 8) > BufferSize )
				Flush();

			m_Buffer[m_Index    ] = (byte) value;
			m_Buffer[m_Index + 1] = (byte)(value >> 8);
			m_Buffer[m_Index + 2] = (byte)(value >> 16);
			m_Buffer[m_Index + 3] = (byte)(value >> 24);
			m_Buffer[m_Index + 4] = (byte)(value >> 32);
			m_Buffer[m_Index + 5] = (byte)(value >> 40);
			m_Buffer[m_Index + 6] = (byte)(value >> 48);
			m_Buffer[m_Index + 7] = (byte)(value >> 56);
			m_Index += 8;
		}

		public override void Write( ulong value )
		{
			if ( (m_Index + 8) > BufferSize )
				Flush();

			m_Buffer[m_Index    ] = (byte) value;
			m_Buffer[m_Index + 1] = (byte)(value >> 8);
			m_Buffer[m_Index + 2] = (byte)(value >> 16);
			m_Buffer[m_Index + 3] = (byte)(value >> 24);
			m_Buffer[m_Index + 4] = (byte)(value >> 32);
			m_Buffer[m_Index + 5] = (byte)(value >> 40);
			m_Buffer[m_Index + 6] = (byte)(value >> 48);
			m_Buffer[m_Index + 7] = (byte)(value >> 56);
			m_Index += 8;
		}

		public override void Write( int value )
		{
			if ( (m_Index + 4) > BufferSize )
				Flush();

			m_Buffer[m_Index    ] = (byte) value;
			m_Buffer[m_Index + 1] = (byte)(value >> 8);
			m_Buffer[m_Index + 2] = (byte)(value >> 16);
			m_Buffer[m_Index + 3] = (byte)(value >> 24);
			m_Index += 4;
		}

		public override void Write( uint value )
		{
			if ( (m_Index + 4) > BufferSize )
				Flush();

			m_Buffer[m_Index    ] = (byte) value;
			m_Buffer[m_Index + 1] = (byte)(value >> 8);
			m_Buffer[m_Index + 2] = (byte)(value >> 16);
			m_Buffer[m_Index + 3] = (byte)(value >> 24);
			m_Index += 4;
		}

		public override void Write( short value )
		{
			if ( (m_Index + 2) > BufferSize )
				Flush();

			m_Buffer[m_Index    ] = (byte) value;
			m_Buffer[m_Index + 1] = (byte)(value >> 8);
			m_Index += 2;
		}

		public override void Write( ushort value )
		{
			if ( (m_Index + 2) > BufferSize )
				Flush();

			m_Buffer[m_Index    ] = (byte) value;
			m_Buffer[m_Index + 1] = (byte)(value >> 8);
			m_Index += 2;
		}

		public override void Write( double value )
		{
			Write(BitConverter.GetBytes(value));
		}

		public override void Write( float value )
		{
			Write(BitConverter.GetBytes(value));
		}

		private char[] m_SingleCharBuffer = new char[1];

		public override void Write( char value )
		{
			if ( (m_Index + 8) > BufferSize )
				Flush();

			m_SingleCharBuffer[0] = value;

			int byteCount = m_Encoding.GetBytes( m_SingleCharBuffer, 0, 1, m_Buffer, m_Index );
			m_Index += byteCount;
		}

		public override void Write( byte value )
		{
			if ( (m_Index + 1) > BufferSize )
				Flush();

			m_Buffer[m_Index++] = value;
		}

		public override void Write( sbyte value )
		{
			if ( (m_Index + 1) > BufferSize )
				Flush();

			m_Buffer[m_Index++] = (byte) value;
		}

		public override void Write( bool value )
		{
			if ( (m_Index + 1) > BufferSize )
				Flush();

			m_Buffer[m_Index++] = (byte) (value ? 1 : 0);
		}

		public override void Write( Point3D value )
		{
			Write( value.m_X );
			Write( value.m_Y );
			Write( value.m_Z );
		}

		public override void Write( Point2D value )
		{
			Write( value.m_X );
			Write( value.m_Y );
		}

		public override void Write( Rectangle2D value )
		{
			Write( value.Start );
			Write( value.End );
		}

		public override void Write( Map value )
		{
			if ( value != null )
				Write( (byte)value.MapIndex );
			else
				Write( (byte)0xFF );
		}

		public override void Write( Item value )
		{
			if ( value == null || value.Deleted )
				Write( Serial.MinusOne );
			else
				Write( value.Serial );
		}

		public override void Write( Mobile value )
		{
			if ( value == null || value.Deleted )
				Write( Serial.MinusOne );
			else
				Write( value.Serial );
		}

		public override void Write( BaseGuild value )
		{
			if ( value == null )
				Write( 0 );
			else
				Write( value.Id );
		}

		public override void WriteMobileList( ArrayList list )
		{
			WriteMobileList( list, false );
		}

		public override void WriteMobileList( ArrayList list, bool tidy )
		{
			if (list == null) {
				Write((int)0);
				return;
			}

			if ( tidy )
			{
				for ( int i = 0; i < list.Count; )
				{
					if ( ((Mobile)list[i]).Deleted )
						list.RemoveAt( i );
					else
						++i;
				}
			}

			Write( list.Count );

			for ( int i = 0; i < list.Count; ++i )
				Write( (Mobile)list[i] );
		}

		public override void WriteItemList( ArrayList list )
		{
			WriteItemList( list, false );
		}

		public override void WriteItemList( ArrayList list, bool tidy )
		{
			if ( tidy )
			{
				for ( int i = 0; i < list.Count; )
				{
					if ( ((Item)list[i]).Deleted )
						list.RemoveAt( i );
					else
						++i;
				}
			}

			Write( list.Count );

			for ( int i = 0; i < list.Count; ++i )
				Write( (Item)list[i] );
		}

		public override void WriteGuildList( ArrayList list )
		{
			WriteGuildList( list, false );
		}

		public override void WriteGuildList( ArrayList list, bool tidy )
		{
			if (list == null) {
				Write((int)0);
				return;
			}

			if ( tidy )
			{
				for ( int i = 0; i < list.Count; )
				{
					if ( ((BaseGuild)list[i]).Disbanded )
						list.RemoveAt( i );
					else
						++i;
				}
			}

			Write( list.Count );

			for ( int i = 0; i < list.Count; ++i )
				Write( (BaseGuild)list[i] );
		}
	}

	public sealed class BinaryFileReader : GenericReader
	{
		private BinaryReader m_File;

		public BinaryFileReader( BinaryReader br ) { m_File = br; }

		public void Close()
		{
			m_File.Close();
		}

		public long Position
		{
			get
			{
				return m_File.BaseStream.Position;
			}
		}

		public long Seek( long offset, SeekOrigin origin )
		{
			return m_File.BaseStream.Seek( offset, origin );
		}

		public override string ReadString()
		{
			if ( ReadByte() != 0 )
				return m_File.ReadString();
			else
				return null;
		}

		public override DateTime ReadDeltaTime()
		{
			long ticks = m_File.ReadInt64();
			long now = Core.Now.Ticks;

			if ( ticks > 0 && (ticks+now) < 0 )
				return DateTime.MaxValue;
			else if ( ticks < 0 && (ticks+now) < 0 )
				return DateTime.MinValue;

			try{ return new DateTime( now+ticks ); }
			catch{ if ( ticks > 0 ) return DateTime.MaxValue; else return DateTime.MinValue; }
		}

		public override IPAddress ReadIPAddress()
		{
			return new IPAddress( m_File.ReadInt64() );
		}

		public override int ReadEncodedInt()
		{
			int v = 0, shift = 0;
			byte b;

			do
			{
				b = m_File.ReadByte();
				v |= (b & 0x7F) << shift;
				shift += 7;
			} while ( b >= 0x80 );

			return v;
		}

		public override DateTime ReadDateTime()
		{
			return new DateTime( m_File.ReadInt64() );
		}

		public override TimeSpan ReadTimeSpan()
		{
			return new TimeSpan( m_File.ReadInt64() );
		}

		public override decimal ReadDecimal()
		{
			return m_File.ReadDecimal();
		}

		public override long ReadLong()
		{
			return m_File.ReadInt64();
		}

		public override ulong ReadULong()
		{
			return m_File.ReadUInt64();
		}

		public override int ReadInt()
		{
			return m_File.ReadInt32();
		}

		public override uint ReadUInt()
		{
			return m_File.ReadUInt32();
		}

		public override short ReadShort()
		{
			return m_File.ReadInt16();
		}

		public override ushort ReadUShort()
		{
			return m_File.ReadUInt16();
		}

		public override double ReadDouble()
		{
			return m_File.ReadDouble();
		}

		public override float ReadFloat()
		{
			return m_File.ReadSingle();
		}

		public override char ReadChar()
		{
			return m_File.ReadChar();
		}

		public override byte ReadByte()
		{
			return m_File.ReadByte();
		}

		public override sbyte ReadSByte()
		{
			return m_File.ReadSByte();
		}

		public override bool ReadBool()
		{
			return m_File.ReadBoolean();
		}

		public override Point3D ReadPoint3D()
		{
			return new Point3D( ReadInt(), ReadInt(), ReadInt() );
		}

		public override Point2D ReadPoint2D()
		{
			return new Point2D( ReadInt(), ReadInt() );
		}

		public override Rectangle2D ReadRect2D()
		{
			return new Rectangle2D( ReadPoint2D(), ReadPoint2D() );
		}

		public override Map ReadMap()
		{
			return Map.Maps[ReadByte()];
		}

		public override Item ReadItem()
		{
			return World.FindItem( ReadInt() );
		}

		public override Mobile ReadMobile()
		{
			return World.FindMobile( ReadInt() );
		}

		public override BaseGuild ReadGuild()
		{
			return BaseGuild.Find( ReadInt() );
		}

		public override ArrayList ReadItemList()
		{
			int count = ReadInt();

			ArrayList list = new ArrayList( count );

			for ( int i = 0; i < count; ++i )
			{
				Item item = ReadItem();

				if ( item != null )
					list.Add( item );
			}

			return list;
		}

		public override ArrayList ReadMobileListOrNull()
		{
			int count = ReadInt();
			if (count == 0)
				return null;

			ArrayList list = new ArrayList( count );

			for ( int i = 0; i < count; ++i )
			{
				Mobile m = ReadMobile();

				if ( m != null )
					list.Add( m );
			}

			return list;
		}

		public override ArrayList ReadMobileList()
		{
			ArrayList list = ReadMobileListOrNull();
			return list == null
				? new ArrayList(2)
				: list;
		}

		public override ArrayList ReadGuildListOrNull()
		{
			int count = ReadInt();
			if (count == 0)
				return null;

			ArrayList list = new ArrayList( count );

			for ( int i = 0; i < count; ++i )
			{
				BaseGuild g = ReadGuild();

				if ( g != null )
					list.Add( g );
			}

			return list;
		}

		public override ArrayList ReadGuildList()
		{
			ArrayList list = ReadGuildListOrNull();
			return list == null
				? new ArrayList(2)
				: list;
		}

		public override bool End()
		{
			return m_File.PeekChar() == -1;
		}
	}

	public sealed class AsyncWriter : GenericWriter
	{
		private static int m_ThreadCount = 0;
		public static int ThreadCount { get { return m_ThreadCount; } }


		private int BufferSize;

		private long m_LastPos, m_CurPos;
		private bool m_Closed;
		private bool PrefixStrings;

		private MemoryStream m_Mem;
		private BinaryWriter m_Bin;
		private FileStream m_File;

		private Queue m_WriteQueue;
		private Thread m_WorkerThread;

		public AsyncWriter( string filename, bool prefix ) : this(filename, 1048576, prefix)//1 mb buffer
		{
		}

		public AsyncWriter( string filename, int buffSize, bool prefix )
		{
			PrefixStrings = prefix;
			m_Closed = false;
			m_WriteQueue = Queue.Synchronized( new Queue() );
			BufferSize = buffSize;

			m_File = new FileStream( filename, FileMode.Create, FileAccess.Write, FileShare.None );
			m_Mem = new MemoryStream( BufferSize + 1024 );
			m_Bin = new BinaryWriter( m_Mem, Utility.UTF8WithEncoding );
		}

		private void Enqueue( MemoryStream mem )
		{
			m_WriteQueue.Enqueue( mem );

			if ( m_WorkerThread == null || !m_WorkerThread.IsAlive )
			{
				m_WorkerThread = new Thread( new ThreadStart( new WorkerThread( this ).Worker ) );
				m_WorkerThread.Priority = ThreadPriority.BelowNormal;
				m_WorkerThread.Start();
			}
		}

		private class WorkerThread
		{
			private AsyncWriter m_Owner;

			public WorkerThread( AsyncWriter owner )
			{
				m_Owner = owner;
			}

			public void Worker()
			{
				AsyncWriter.m_ThreadCount++;
				while ( m_Owner.m_WriteQueue.Count > 0 )
				{
					MemoryStream mem = (MemoryStream)m_Owner.m_WriteQueue.Dequeue();

					if ( mem != null && mem.Length > 0 )
						mem.WriteTo( m_Owner.m_File );
				}

				if ( m_Owner.m_Closed )
					m_Owner.m_File.Close();
				AsyncWriter.m_ThreadCount--;
			}
		}

		private void OnWrite()
		{
			long curlen = m_Mem.Length;
			m_CurPos += curlen - m_LastPos;
			m_LastPos = curlen;
			if ( curlen >= BufferSize )
			{
				Enqueue( m_Mem );
				m_Mem = new MemoryStream( BufferSize + 1024 );
				m_Bin = new BinaryWriter( m_Mem, Utility.UTF8WithEncoding );
				m_LastPos = 0;
			}
		}

		public MemoryStream MemStream
		{
			get
			{
				return m_Mem;
			}
			set
			{
				if ( m_Mem.Length > 0 )
					Enqueue( m_Mem );

				m_Mem = value;
				m_Bin = new BinaryWriter( m_Mem, Utility.UTF8WithEncoding );
				m_LastPos = 0;
				m_CurPos = m_Mem.Length;
				m_Mem.Seek( 0, SeekOrigin.End );
			}
		}

		public override void Close()
		{
			Enqueue( m_Mem );
			m_Closed = true;
		}

		public override long Position
		{
			get
			{
				return m_CurPos;
			}
		}

		public override void Write( IPAddress value )
		{
			m_Bin.Write( value.Address );
			OnWrite();
		}

		public override void Write( string value )
		{
			if ( PrefixStrings )
			{
				if ( value == null )
				{
					m_Bin.Write( (byte)0 );
				}
				else
				{
					m_Bin.Write( (byte)1 );
					m_Bin.Write( value );
				}
			}
			else
			{
				m_Bin.Write( value );
			}
			OnWrite();
		}

		public override void WriteDeltaTime( DateTime value )
		{
			long ticks = value.Ticks;
			long now = Core.Now.Ticks;

			TimeSpan d;

			try{ d = new TimeSpan( ticks-now ); }
			catch{ if ( ticks < now ) d = TimeSpan.MaxValue; else d = TimeSpan.MaxValue; }

			Write( d );
		}

		public override void Write( DateTime value )
		{
			m_Bin.Write( value.Ticks );
			OnWrite();
		}

		public override void Write( TimeSpan value )
		{
			m_Bin.Write( value.Ticks );
			OnWrite();
		}

		public override void Write( decimal value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( long value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( ulong value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void WriteEncodedInt( int value )
		{
			uint v = (uint) value;

			while ( v >= 0x80 )
			{
				m_Bin.Write( (byte) (v | 0x80) );
				v >>= 7;
			}

			m_Bin.Write( (byte) v);
			OnWrite();
		}

		public override void Write( int value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( uint value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( short value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( ushort value)
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( double value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( float value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( char value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( byte value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( sbyte value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( bool value )
		{
			m_Bin.Write( value );
			OnWrite();
		}

		public override void Write( Point3D value )
		{
			Write( value.m_X );
			Write( value.m_Y );
			Write( value.m_Z );
		}

		public override void Write( Point2D value )
		{
			Write( value.m_X );
			Write( value.m_Y );
		}

		public override void Write( Rectangle2D value )
		{
			Write( value.Start );
			Write( value.End );
		}

		public override void Write( Map value )
		{
			if ( value != null )
				Write( (byte)value.MapIndex );
			else
				Write( (byte)0xFF );
		}

		public override void Write( Item value )
		{
			if ( value == null || value.Deleted )
				Write( Serial.MinusOne );
			else
				Write( value.Serial );
		}

		public override void Write( Mobile value )
		{
			if ( value == null || value.Deleted )
				Write( Serial.MinusOne );
			else
				Write( value.Serial );
		}

		public override void Write( BaseGuild value )
		{
			if ( value == null )
				Write( 0 );
			else
				Write( value.Id );
		}

		public override void WriteMobileList( ArrayList list )
		{
			WriteMobileList( list, false );
		}

		public override void WriteMobileList( ArrayList list, bool tidy )
		{
			if ( tidy )
			{
				for ( int i = 0; i < list.Count; )
				{
					if ( ((Mobile)list[i]).Deleted )
						list.RemoveAt( i );
					else
						++i;
				}
			}

			Write( list.Count );

			for ( int i = 0; i < list.Count; ++i )
				Write( (Mobile)list[i] );
		}

		public override void WriteItemList( ArrayList list )
		{
			WriteItemList( list, false );
		}

		public override void WriteItemList( ArrayList list, bool tidy )
		{
			if ( tidy )
			{
				for ( int i = 0; i < list.Count; )
				{
					if ( ((Item)list[i]).Deleted )
						list.RemoveAt( i );
					else
						++i;
				}
			}

			Write( list.Count );

			for ( int i = 0; i < list.Count; ++i )
				Write( (Item)list[i] );
		}

		public override void WriteGuildList( ArrayList list )
		{
			WriteGuildList( list, false );
		}

		public override void WriteGuildList( ArrayList list, bool tidy )
		{
			if ( tidy )
			{
				for ( int i = 0; i < list.Count; )
				{
					if ( ((BaseGuild)list[i]).Disbanded )
						list.RemoveAt( i );
					else
						++i;
				}
			}

			Write( list.Count );

			for ( int i = 0; i < list.Count; ++i )
				Write( (BaseGuild)list[i] );
		}
	}
}