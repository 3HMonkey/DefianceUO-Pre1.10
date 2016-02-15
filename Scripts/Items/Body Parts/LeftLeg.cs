using System;
using Server;

namespace Server.Items
{
	public class LeftLeg : BaseBodyPart
	{
		[Constructable]
		public LeftLeg() : base( 0x1DA3 )
		{
		}

		public LeftLeg( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}