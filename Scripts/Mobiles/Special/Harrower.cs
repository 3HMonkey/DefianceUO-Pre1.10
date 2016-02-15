using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Engines.CannedEvil;

namespace Server.Mobiles
{
	public class Harrower : BaseCreature
	{
		private bool m_TrueForm;
		private Item m_GateItem;
		private ArrayList m_Tentacles;
		private Timer m_Timer;

		private class SpawnEntry
		{
			public Point3D m_Location;
			public Point3D m_Entrance;

			public SpawnEntry( Point3D loc, Point3D ent )
			{
				m_Location = loc;
				m_Entrance = ent;
			}
		}

		private static SpawnEntry[] m_Entries = new SpawnEntry[]
		{
			new SpawnEntry( new Point3D( 5284, 798, 0 ), new Point3D( 1176, 2638, 0 ) ),
			new SpawnEntry( new Point3D( 5751, 1297, 0 ), new Point3D( 5765, 2914, 34 ) ),
			new SpawnEntry( new Point3D( 5594, 241, 0 ), new Point3D( 509, 1569, 0 ) ),
			new SpawnEntry( new Point3D( 5519, 898, 30 ), new Point3D( 1300, 1073, 0 ) ),
			new SpawnEntry( new Point3D( 5306, 675, -20 ), new Point3D( 4102, 435, 2 ) ),
			new SpawnEntry( new Point3D( 6082, 67, 27 ), new Point3D( 4725, 3826, 0 ) )
		};


		private static ArrayList m_Instances = new ArrayList();

		public static ArrayList Instances{ get{ return m_Instances; } }

		public static Harrower Spawn( Point3D platLoc, Map platMap )
		{
			if ( m_Instances.Count > 0 )
				return null;

			SpawnEntry entry = m_Entries[Utility.Random( m_Entries.Length )];

			Harrower harrower = new Harrower();

			harrower.MoveToWorld( entry.m_Location, Map.Felucca );

			harrower.m_GateItem = new HarrowerGate( harrower, platLoc, platMap, entry.m_Entrance, Map.Felucca );

			return harrower;
		}

		public static bool CanSpawn
		{
			get
			{
				return ( m_Instances.Count == 0 );
			}
		}

		[Constructable]
		public Harrower() : base( AIType.AI_Mage, FightMode.Closest, 18, 1, 0.2, 0.4 )
		{
			m_Instances.Add( this );

			Name = "the harrower";
			BodyValue = 146;

			SetStr( 900, 1000 );
			SetDex( 125, 135 );
			SetInt( 1000, 1200 );

			SetFameLevel( 5 );
			SetKarmaLevel( 5 );

			VirtualArmor = 60;

			SetDamageType( ResistanceType.Physical, 50 );
			SetDamageType( ResistanceType.Energy, 50 );

			SetResistance( ResistanceType.Physical, 55, 65 );
			SetResistance( ResistanceType.Fire, 60, 80 );
			SetResistance( ResistanceType.Cold, 60, 80 );
			SetResistance( ResistanceType.Poison, 60, 80 );
			SetResistance( ResistanceType.Energy, 60, 80 );

			SetSkill( SkillName.Wrestling, 93.9, 96.5 );
			SetSkill( SkillName.Tactics, 96.9, 102.2 );
			SetSkill( SkillName.MagicResist, 131.4, 140.8 );
			SetSkill( SkillName.Magery, 156.2, 161.4 );
			SetSkill( SkillName.EvalInt, 100.0 );
			SetSkill( SkillName.Meditation, 120.0 );

			m_Tentacles = new ArrayList();

			m_Timer = new TeleportTimer( this );
			m_Timer.Start();
		}

		public override void GenerateLoot()
		{
			AddLoot( LootPack.SuperBoss, 2 );
			AddLoot( LootPack.Meager );
		}

		public override bool Unprovokable{ get{ return true; } }
		public override Poison PoisonImmune{ get{ return Poison.Lethal; } }

		private double[] m_Offsets = new double[]
			{
				Math.Cos( 000.0 / 180.0 * Math.PI ), Math.Sin( 000.0 / 180.0 * Math.PI ),
				Math.Cos( 040.0 / 180.0 * Math.PI ), Math.Sin( 040.0 / 180.0 * Math.PI ),
				Math.Cos( 080.0 / 180.0 * Math.PI ), Math.Sin( 080.0 / 180.0 * Math.PI ),
				Math.Cos( 120.0 / 180.0 * Math.PI ), Math.Sin( 120.0 / 180.0 * Math.PI ),
				Math.Cos( 160.0 / 180.0 * Math.PI ), Math.Sin( 160.0 / 180.0 * Math.PI ),
				Math.Cos( 200.0 / 180.0 * Math.PI ), Math.Sin( 200.0 / 180.0 * Math.PI ),
				Math.Cos( 240.0 / 180.0 * Math.PI ), Math.Sin( 240.0 / 180.0 * Math.PI ),
				Math.Cos( 280.0 / 180.0 * Math.PI ), Math.Sin( 280.0 / 180.0 * Math.PI ),
				Math.Cos( 320.0 / 180.0 * Math.PI ), Math.Sin( 320.0 / 180.0 * Math.PI ),
			};

		public void Morph()
		{
			if ( m_TrueForm )
				return;

			m_TrueForm = true;

			Name = "the true harrower";
			BodyValue = 780;
			Hue = 0x497;

			Hits = HitsMax;
			Stam = StamMax;
			Mana = ManaMax;

			ProcessDelta();

			Say( 1049499 ); // Behold my true form!

			Map map = this.Map;

			if ( map != null )
			{
				for ( int i = 0; i < m_Offsets.Length; i += 2 )
				{
					double rx = m_Offsets[i];
					double ry = m_Offsets[i + 1];

					int dist = 0;
					bool ok = false;
					int x = 0, y = 0, z = 0;

					while ( !ok && dist < 10 )
					{
						int rdist = 10 + dist;

						x = this.X + (int)(rx * rdist);
						y = this.Y + (int)(ry * rdist);
						z = map.GetAverageZ( x, y );

						if ( !(ok = map.CanFit( x, y, this.Z, 16, false, false ) ) )
							ok = map.CanFit( x, y, z, 16, false, false );

						if ( dist >= 0 )
							dist = -(dist + 1);
						else
							dist = -(dist - 1);
					}

					if ( !ok )
						continue;

					BaseCreature spawn = new HarrowerTentacles( this );

					spawn.Team = this.Team;

					spawn.MoveToWorld( new Point3D( x, y, z ), map );

					m_Tentacles.Add( spawn );
				}
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public override int HitsMax{ get{ return m_TrueForm ? 65000 : 30000; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public override int ManaMax{ get{ return 5000; } }

		public Harrower( Serial serial ) : base( serial )
		{
			m_Instances.Add( this );
		}

		public override void OnAfterDelete()
		{
			m_Instances.Remove( this );

			base.OnAfterDelete();
		}

		public override bool DisallowAllMoves{ get{ return m_TrueForm; } }

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_TrueForm );
			writer.Write( m_GateItem );
			writer.WriteMobileList( m_Tentacles );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_TrueForm = reader.ReadBool();
					m_GateItem = reader.ReadItem();
					m_Tentacles = reader.ReadMobileList();

					m_Timer = new TeleportTimer( this );
					m_Timer.Start();

					break;
				}
			}
		}

		public void GivePowerScrolls()
		{
			ArrayList toGive = new ArrayList();

			ArrayList list = Aggressors;
			for ( int i = 0; i < list.Count; ++i )
			{
				AggressorInfo info = (AggressorInfo)list[i];

				if ( info.Attacker.Player && info.Attacker.Alive && (DateTime.Now - info.LastCombatTime) < TimeSpan.FromSeconds( 30.0 ) && !toGive.Contains( info.Attacker ) )
					toGive.Add( info.Attacker );
			}

			list = Aggressed;
			for ( int i = 0; i < list.Count; ++i )
			{
				AggressorInfo info = (AggressorInfo)list[i];

				if ( info.Defender.Player && info.Defender.Alive && (DateTime.Now - info.LastCombatTime) < TimeSpan.FromSeconds( 30.0 ) && !toGive.Contains( info.Defender ) )
					toGive.Add( info.Defender );
			}

			if ( toGive.Count == 0 )
				return;

			// Randomize
			for ( int i = 0; i < toGive.Count; ++i )
			{
				int rand = Utility.Random( toGive.Count );
				object hold = toGive[i];
				toGive[i] = toGive[rand];
				toGive[rand] = hold;
			}

			for ( int i = 0; i < 16; ++i )
			{
				int level;
				double random = Utility.RandomDouble();

				if ( 0.1 >= random )
					level = 25;
				else if ( 0.25 >= random )
					level = 20;
				else if ( 0.45 >= random )
					level = 15;
				else if ( 0.70 >= random )
					level = 10;
				else
					level = 5;

				Mobile m = (Mobile)toGive[i % toGive.Count];

				HarrowerTicket ps  = new HarrowerTicket();

				m.SendLocalizedMessage( 1049524 ); // You have received a scroll of power!
				m.AddToBackpack( ps );

				if ( m is PlayerMobile )
				{
					PlayerMobile pm = (PlayerMobile)m;

					for ( int j = 0; j < pm.JusticeProtectors.Count; ++j )
					{
						Mobile prot = (Mobile)pm.JusticeProtectors[j];

						if ( prot.Map != m.Map || prot.Kills >= 5 || prot.Criminal )
							continue;

						int chance = 0;

						switch ( VirtueHelper.GetLevel( prot, VirtueName.Justice ) )
						{
							case VirtueLevel.Seeker: chance = 60; break;
							case VirtueLevel.Follower: chance = 80; break;
							case VirtueLevel.Knight: chance = 100; break;
						}

						if ( chance > Utility.Random( 100 ) )
						{
							prot.SendLocalizedMessage( 1049368 ); // You have been rewarded for your dedication to Justice!
							//prot.AddToBackpack( new StatCapScroll( 225 + level ) );
						}
					}
				}
			}
		}

		public override bool OnBeforeDeath()
		{
			if ( m_TrueForm )
			{
				if ( !NoKillAwards )
				{
					GivePowerScrolls();

					Map map = this.Map;

					if ( map != null )
					{
						for ( int x = -16; x <= 16; ++x )
						{
							for ( int y = -16; y <= 16; ++y )
							{
								double dist = Math.Sqrt(x*x+y*y);

								if ( dist <= 16 )
									new GoodiesTimer( map, X + x, Y + y ).Start();
							}
						}
					}

					for ( int i = 0; i < m_Tentacles.Count; ++i )
					{
						Mobile m = (Mobile)m_Tentacles[i];

						if ( !m.Deleted )
							m.Kill();
					}

					m_Tentacles.Clear();

					if ( m_GateItem != null )
						m_GateItem.Delete();
				}

				return base.OnBeforeDeath();
			}
			else
			{
				Morph();
				return false;
			}
		}

		private class TeleportTimer : Timer
		{
			private Mobile m_Owner;

			private static int[] m_Offsets = new int[]
			{
				-1, -1,
				-1,  0,
				-1,  1,
				0, -1,
				0,  1,
				1, -1,
				1,  0,
				1,  1
			};

			public TeleportTimer( Mobile owner ) : base( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromSeconds( 5.0 ) )
			{
				m_Owner = owner;
			}

			protected override void OnTick()
			{
				if ( m_Owner.Deleted )
				{
					Stop();
					return;
				}

				Map map = m_Owner.Map;

				if ( map == null )
					return;

				if ( 0.25 < Utility.RandomDouble() )
					return;

				Mobile toTeleport = null;

				foreach ( Mobile m in m_Owner.GetMobilesInRange( 16 ) )
				{
					if ( m != m_Owner && m.Player && m_Owner.CanBeHarmful( m ) && m_Owner.CanSee( m ) )
					{
						toTeleport = m;
						break;
					}
				}

				if ( toTeleport != null )
				{
					int offset = Utility.Random( 8 ) * 2;

					Point3D to = m_Owner.Location;

					for ( int i = 0; i < m_Offsets.Length; i += 2 )
					{
						int x = m_Owner.X + m_Offsets[(offset + i) % m_Offsets.Length];
						int y = m_Owner.Y + m_Offsets[(offset + i + 1) % m_Offsets.Length];

						if ( map.CanSpawnMobile( x, y, m_Owner.Z ) )
						{
							to = new Point3D( x, y, m_Owner.Z );
							break;
						}
						else
						{
							int z = map.GetAverageZ( x, y );

							if ( map.CanSpawnMobile( x, y, z ) )
							{
								to = new Point3D( x, y, z );
								break;
							}
						}
					}

					Mobile m = toTeleport;

					Point3D from = m.Location;

					m.Location = to;

					Server.Spells.SpellHelper.Turn( m_Owner, toTeleport );
					Server.Spells.SpellHelper.Turn( toTeleport, m_Owner );

					m.ProcessDelta();

					Effects.SendLocationParticles( EffectItem.Create( from, m.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 2023 );
					Effects.SendLocationParticles( EffectItem.Create(   to, m.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 5023 );

					m.PlaySound( 0x1FE );

					m_Owner.Combatant = toTeleport;
				}
			}
		}

		private class GoodiesTimer : Timer
		{
			private Map m_Map;
			private int m_X, m_Y;

			public GoodiesTimer( Map map, int x, int y ) : base( TimeSpan.FromSeconds( Utility.RandomDouble() * 10.0 ) )
			{
				m_Map = map;
				m_X = x;
				m_Y = y;
			}

			protected override void OnTick()
			{
				int z = m_Map.GetAverageZ( m_X, m_Y );
				bool canFit = m_Map.CanFit( m_X, m_Y, z, 6, false, false );

				for ( int i = -3; !canFit && i <= 3; ++i )
				{
					canFit = m_Map.CanFit( m_X, m_Y, z + i, 6, false, false );

					if ( canFit )
						z += i;
				}

				if ( !canFit )
					return;

				Gold g = new Gold( 750, 1250 );

				g.MoveToWorld( new Point3D( m_X, m_Y, z ), m_Map );

				if ( 0.5 >= Utility.RandomDouble() )
				{
					switch ( Utility.Random( 3 ) )
					{
						case 0: // Fire column
						{
							Effects.SendLocationParticles( EffectItem.Create( g.Location, g.Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 5052 );
							Effects.PlaySound( g, g.Map, 0x208 );

							break;
						}
						case 1: // Explosion
						{
							Effects.SendLocationParticles( EffectItem.Create( g.Location, g.Map, EffectItem.DefaultDuration ), 0x36BD, 20, 10, 5044 );
							Effects.PlaySound( g, g.Map, 0x307 );

							break;
						}
						case 2: // Ball of fire
						{
							Effects.SendLocationParticles( EffectItem.Create( g.Location, g.Map, EffectItem.DefaultDuration ), 0x36FE, 10, 10, 5052 );

							break;
						}
					}
				}
			}
		}
	}
}