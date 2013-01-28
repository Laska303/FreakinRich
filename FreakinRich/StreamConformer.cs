using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreakinRich
{
    static class StreamConformer
    {
        public enum MessageType {   SET_PLAYER_NAME = 0, 
                                    UPDATE_PLAYERS_NAMES, 
                                    UPDATE_PLAYERS_COUNT, 
                                    ENABLE_DICE_ROLL, 
                                    SET_DICE_VALUE, 
                                    UPDATE_CURRENT_PLAYER, 
                                    UPDATE_PLAYER_POSITION, 
                                    PROPERTY_ON_SALE, 
                                    CHARGE_RENT,  
                                    JAIL_POSITION, 
                                    CHANCE_POSITION ,
                                    TAX_POSITION, 
                                    BUY_PROPERTY,  
                                    ENABLE_GROUP_HOUSES,
                                    UPDATE_OWNER,  
                                    PLAYER_HASNO_MONEY_2BUY, 
                                    SHOW_DICE_VALUES, 
                                    PAY_RENT,  
                                    UPDATE_MONEY, 
                                    PLAYER_HASNO_MONEY_2PAY,
                                    PAY_TAX,  
                                    GO_TO_JAIL, 
                                    SET_MARKER_ORDER,
                                    IS_DOUBLE, 
                                    TIMES3_DOUBLE,
                                    TAKE_CHANCE_ACTION,
                                    CHECK_IFIS_OWNER,
                                    ENABLE_SELL_PROPERTY,
                                    SELL_PROPERTY,
                                    SELL_HOUSE,
                                    BUY_HOUSE,
                                    QUERY_HOUSES,
                                    CAN_BUY_HOUSE,
                                    CAN_SELL_HOUSE,
                                    CAN_BUY_SELL_HOUSE,
                                    PLAYERS_FULL,
                                    PLAYER_HAS_HOUSES_2SELL,
									DISABLE_GROUP_HOUSES,
                                    GAME_IS_OVER,
                                    OTHERS_TURN,
                                    CHAT,
                                    PING,
                                    PONG,
                                    CONNECTION_FAILED
        }

        public static byte[] Encode(MessageType type, String message)
        {
            String encodedMsg = ((int)type).ToString("00") + message + (char)3;

            //transforma de String para array de bytes  
            ASCIIEncoding encoder = new ASCIIEncoding();
            return encoder.GetBytes(encodedMsg);
        }

        public static MessageType Decode(byte[] streamData, int byteCount, out String message)
        {
            //transforma de array de bytes para String
            ASCIIEncoding encoder = new ASCIIEncoding();
            String msg = encoder.GetString(streamData, 0, byteCount);
            
            //remove char endOfText
            msg = msg.Remove(msg.Length - 1);
            message = msg.Substring(2);
            return (MessageType)Int32.Parse(msg.Substring(0, 2));
        }
    }
}
