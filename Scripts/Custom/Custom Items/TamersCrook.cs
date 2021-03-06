using System;
using System.Collections;
using Server.Items;
using Server.ContextMenus;

namespace Server.Items
{
	[FlipableAttribute( 0xE81, 0xE82 )]
	public class TamersCrook : BaseStaff
	{
		private SkillName m_Skill1 = SkillName.AnimalLore;
		private SkillName m_Skill2 = SkillName.AnimalTaming;

		private SkillMod m_SkillMod1;
		private SkillMod m_SkillMod2;

		[Constructable]
		public TamersCrook() : base( 0xE81 )
		{
			Hue = 2213;
                        ItemID = 0xE81;
                        Weight = 1.0;
			LootType = LootType.Newbied;
			Name = "a tamers crook";
		}

		public override void OnSingleClick( Mobile from )
		{
			this.LabelTo( from, Name + " [" + m_Skill1.ToString() + "/" + m_Skill2.ToString() + "]" );
		}

		public TamersCrook( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
			writer.Write( (int) m_Skill1 );
			writer.Write( (int) m_Skill2 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
			m_Skill1 = (SkillName)reader.ReadInt();
			m_Skill2 = (SkillName)reader.ReadInt();
			if (Parent is Mobile)
				OnAdded( Parent );
		}

		public override void OnAdded( object parent )
		{
			base.OnAdded( parent );

			if ( parent is Mobile )
			{
				Mobile from = parent as Mobile;
				if ( m_SkillMod1 != null )
					m_SkillMod1.Remove();
				if (m_SkillMod2 != null )
					m_SkillMod2.Remove();
				int amount = 10;
				if (from.SkillsCap < from.SkillsTotal + (amount * 10))
					amount = (from.SkillsCap - from.SkillsTotal) / 10;
				m_SkillMod1 = new DefaultSkillMod( m_Skill1, true, amount );
				m_SkillMod1.ObeyCap = true;
				from.AddSkillMod( m_SkillMod1 );
				amount = 10;
				if (from.SkillsCap < from.SkillsTotal + (amount * 10))
					amount = (from.SkillsCap - from.SkillsTotal) / 10;
				m_SkillMod2 = new DefaultSkillMod( m_Skill2, true, amount );
				m_SkillMod2.ObeyCap = true;
				from.AddSkillMod( m_SkillMod2 );
			}
		}

		public override void OnRemoved( object parent )
		{
			base.OnRemoved( parent );

			if ( m_SkillMod1 != null )
				m_SkillMod1.Remove();
			if ( m_SkillMod2 != null )
				m_SkillMod2.Remove();

			m_SkillMod1 = null;
			m_SkillMod2 = null;
		}
	}
}