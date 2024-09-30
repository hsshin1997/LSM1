using System;
using System.Collections.Generic; 
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using LSM.CrcLibrary;
using System.IO;

class LSMSocketDataAcquisition
{
    static void Main(string[] args)
    {
        // LSM Motor IP Address and Port
        string ipAddress = "192.168.1.11";
        int port = 800; // port 799

        try
        {
            // Create a TCP Client
            TcpClient client = new TcpClient();
            Console.WriteLine($"Connecting to LSM motor at {ipAddress}:{port}");

            // connect to the LSM motor
            client.Connect(ipAddress, port);

            Console.WriteLine("Connected to LSM motor");

            // Get the network stream for reading and writing
            NetworkStream stream = client.GetStream();

            // Set read and write timeouts (in milliseconds)
            stream.ReadTimeout = 5000;
            stream.WriteTimeout = 5000;

            // Example command to send to the motor
            // string command = "Status_Request";

            // Construct the message
            byte messageType = 0xB5; // Replace with the actual message type
            byte[] data = new byte[] { 0x01, 0x00, 0x00 }; // Replace with actual data if any

            byte[] message = ConstructMessage(messageType, data);

            byte[] crc = CrcCcitt.ComputeChecksum(message);

            byte[] messageWithCrc = new byte[message.Length + crc.Length];
            Array.Copy(message, 0, messageWithCrc, 0, message.Length);
            Array.Copy(crc, 0, messageWithCrc, message.Length, crc.Length);

            // Send the command to the motor
            Console.WriteLine("Sending command to LSM motor...");

            // Write the command bytes to the network stream
            stream.Write(messageWithCrc, 0, messageWithCrc.Length);

            // DEBUG: console write
            foreach (byte b in messageWithCrc)
            {
                Console.WriteLine($"0x{b:X2}");
            }

            // Create a list to store the full response
            List<byte> fullResponse = new List<byte>();

            // Buffer to store incoming bytes
            byte[] buffer = new byte[256];

            try
            {
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Console.WriteLine($"Bytes read: {bytesRead}");
                    // Add the received bytes to the full response
                    fullResponse.AddRange(buffer.Take(bytesRead));

                    // Check if all data has been received
                    if (!stream.DataAvailable)
                    {
                        break;
                    }
                }

                byte[] responseBytes = fullResponse.ToArray();

                Console.WriteLine(BitConverter.ToString(responseBytes));


                // TODO: Process the response bytes
                // ProcessResponse(responseBytes);
            }
            catch (IOException ex)
            {
                // Handle read timeout or other IO exceptions
                Console.WriteLine($"IOException: {ex.Message}");
            }

            // close the stream and client
            stream.Close();
            client.Close();
            Console.WriteLine("Connection Closed");
        }
        catch (SocketException se)
        {
            Console.WriteLine($"SocketException: {se.SocketErrorCode} - {se.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
        }

        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }

    // Message construction
    static byte[] ConstructMessage(byte messageType, byte[] data)
    {
        /*
        Message Structure:
        Item            When Used   Size(bytes)     Value / Range
        Start Bytes     Always      2               0xAB followed by 0xBA
        Message Length  Always      1               Length of the message following the length byte, including CRC bytes(Range: 3 to 61)
        Message Type    Always      1               0 to 255
        Data            Optional    0 to 58         Varies depending upon the command
        CRC             Always      2               0 to 65535
        */

        // Message header
        byte[] startBytes = new byte[] { 0xAB, 0xBA };

        // Calculate Message Length
        // Length = Size of Message Type (1) + Size of Data + Size of CRC (2)
        int messageLength = 1 + data.Length + 2;
        if (messageLength < 3 || messageLength > 61)
        {
            throw new ArgumentException("Invalid message length. Must be between 3 and 61 bytes.");
        }

        // Message Legnth byte (1 byte)
        byte lengthByte = (byte)messageLength;

        // Message Type
        byte[] messageTypeByte = new byte[] { messageType };

        // Calculate CRC over Message Type and Data
        //byte[] crc = CalculateCRC(messageTypeByte.Concat(data).ToArray());

        // Construct the full message
        List<byte> message = new List<byte>();
        message.AddRange(startBytes);
        message.Add(lengthByte);
        message.Add(messageType);
        message.AddRange(data);
        //message.AddRange(crc);

        // Display the message bytes
        Console.WriteLine($"Constructed Message: {BitConverter.ToString(message.ToArray())}");


        return message.ToArray();
    }
}

//using System;
//using System.Net.Sockets;
//using System.Text;
//using System.Xml.Serialization;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//class LsmSocketDataAcquisitionCon
//{
//    static void Main(string[] args)
//    {
//        // LSM Motor IP Address and Port
//        string ipAddress = "192.168.1.11";
//        int port = 798; // port 799

//    }



//    static byte[] CalculateCRC(byte[] data)
//    {
//        ushort crc = 0xFFFF; // Initial value

//        foreach (byte b in data)
//        {
//            crc ^= (ushort)(b << 8); // Align byte with MSB of CRC

//            for (int i = 0; i < 8; i++)
//            {
//                if ((crc & 0x8000) != 0)
//                {
//                    crc = (ushort)((crc << 1) ^ 0x1021);
//                }
//                else
//                {
//                    crc <<= 1;
//                }
//            }
//        }

//        // Return CRC as two bytes (big-endian)
//        byte crcHigh = (byte)((crc >> 8) & 0xFF);
//        byte crcLow = (byte)(crc & 0xFF);
//        return new byte[] { crcHigh, crcLow };
//    }

//    static byte[] ReceiveResponse(NetworkStream stream)
//    {
//        List<byte> response = new List<byte>();
//        byte[] buffer = new byte[256];

//        try
//        {
//            // Read Start Bytes
//            int bytesRead = stream.Read(buffer, 0, 2);
//            if (bytesRead != 2 || buffer[0] != 0xAB || buffer[1] != 0xBA)
//            {
//                Console.WriteLine("Invalid start bytes in response.");
//                return null;
//            }
//            response.AddRange(buffer.Take(2));

//            // Read Length Byte
//            bytesRead = stream.Read(buffer, 0, 1);
//            if (bytesRead != 1)
//            {
//                Console.WriteLine("Failed to read length byte.");
//                return null;
//            }
//            byte lengthByte = buffer[0];
//            response.Add(lengthByte);

//            // Read the rest of the message
//            int remainingBytes = lengthByte;
//            while (remainingBytes > 0)
//            {
//                bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, remainingBytes));
//                if (bytesRead == 0)
//                {
//                    Console.WriteLine("Connection closed by remote host.");
//                    return null;
//                }
//                response.AddRange(buffer.Take(bytesRead));
//                remainingBytes -= bytesRead;
//            }

//            // Display the response bytes
//            Console.WriteLine($"Received Response: {BitConverter.ToString(response.ToArray())}");

//            return response.ToArray();
//        }
//        catch (IOException ex)
//        {
//            Console.WriteLine($"IOException: {ex.Message}");
//            return null;
//        }
//    }

//    static void ParseResponse(byte[] response)
//    {
//        // Verify Start Bytes
//        if (response[0] != 0xAB || response[1] != 0xBA)
//        {
//            Console.WriteLine("Invalid start bytes in response.");
//            return;
//        }

//        // Get Length Byte
//        byte lengthByte = response[2];
//        int expectedLength = lengthByte + 3; // Length of message including start bytes and length byte

//        if (response.Length != expectedLength)
//        {
//            Console.WriteLine("Response length does not match length byte.");
//            return;
//        }

//        // Extract Message Type, Data, and CRC
//        byte messageType = response[3];
//        int dataLength = lengthByte - 1 - 2; // Subtract Message Type and CRC size
//        byte[] data = new byte[dataLength];
//        Array.Copy(response, 4, data, 0, dataLength);

//        byte crcHigh = response[response.Length - 2];
//        byte crcLow = response[response.Length - 1];
//        byte[] receivedCrc = new byte[] { crcHigh, crcLow };

//        // Calculate CRC over Message Type and Data
//        byte[] calculatedCrc = CalculateCRC(new byte[] { messageType }.Concat(data).ToArray());

//        // Verify CRC
//        if (!receivedCrc.SequenceEqual(calculatedCrc))
//        {
//            Console.WriteLine("CRC check failed.");
//            return;
//        }

//        // Display Parsed Information
//        Console.WriteLine("Response Parsed Successfully:");
//        Console.WriteLine($"  Message Type: {messageType}");
//        Console.WriteLine($"  Data: {BitConverter.ToString(data)}");

//        // Further processing based on message type
//        ProcessResponseData(messageType, data);
//    }

//    static void ProcessResponseData(byte messageType, byte[] data)
//    {
//        // Implement specific parsing based on the message type
//        // For example, if messageType == 0xD7, parse the motor status

//        if (messageType == 0xD7)
//        {
//            // Assuming data format as per previous messages
//            int messageLength = 17; // Each motor status message is 17 bytes long

//            int offset = 0;
//            while (offset + messageLength <= data.Length)
//            {
//                byte[] message = new byte[messageLength];
//                Array.Copy(data, offset, message, 0, messageLength);
//                ParseMotorStatusMessage(message);
//                offset += messageLength;
//            }
//        }
//        else
//        {
//            Console.WriteLine("Unknown message type.");
//        }
//    }

//    static void ParseMotorStatusMessage(byte[] message)
//    {
//        if (message.Length != 17)
//        {
//            Console.WriteLine("Invalid motor status message length.");
//            return;
//        }

//        // Parse the message
//        // Bytes 0-1: Path ID (big-endian)
//        ushort pathId = (ushort)((message[0] << 8) | message[1]);

//        // Byte 2: Path Status
//        byte pathStatus = message[2];

//        // Byte 3: Motor Position on Path
//        byte motorPosition = message[3];

//        // Byte 4: Motor Presence
//        byte motorPresence = message[4];

//        // Byte 5: Motor Type
//        byte motorType = message[5];

//        // Bytes 6-15: Fault Data
//        byte[] faultData = new byte[10];
//        Array.Copy(message, 6, faultData, 0, 10);

//        // Display the parsed information
//        Console.WriteLine($"Motor Status Message:");
//        Console.WriteLine($"  Path ID: {pathId}");
//        Console.WriteLine($"  Path Status: {pathStatus}");
//        Console.WriteLine($"  Motor Position: {motorPosition}");
//        Console.WriteLine($"  Motor Presence: {motorPresence}");
//        Console.WriteLine($"  Motor Type: {motorType}");
//        Console.WriteLine($"  Fault Data: {BitConverter.ToString(faultData)}");
//    }
//}


//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net.Sockets;

//class LsmSocketDataAcquisitionCon
//{
//    static void Main(string[] args)
//    {
//        // LSM Motor IP Address and Port
//        string ipAddress = "192.168.1.11";
//        int port = 800;

//        try
//        {
//            // Create a TCP Client
//            TcpClient client = new TcpClient();
//            Console.WriteLine($"Connecting to LSM motor at {ipAddress}:{port}");

//            // Connect to the LSM motor
//            client.Connect(ipAddress, port);
//            Console.WriteLine("Connected to LSM motor");

//            // Get the network stream for reading and writing
//            NetworkStream stream = client.GetStream();

//            // Set read and write timeouts (in milliseconds)
//            stream.ReadTimeout = 5000;
//            stream.WriteTimeout = 5000;

//            // Construct the message
//            //byte messageType = 0x00; // Replace with the actual message type
//            //byte[] data = new byte[] { }; // Replace with actual data if any

//            //byte[] message = ConstructMessage(messageType, data);

//            //// Send the message to the motor
//            //Console.WriteLine("Sending message to LSM motor...");
//            //stream.Write(message, 0, message.Length);

//            // TEMP ================================================
//            byte[] status_request = new byte[5];
//            status_request[0] = 0xB5; // Command Identifier
//            status_request[1] = 0x05; // Request Type
//            status_request[2] = 0x00; // Path ID (Use 0 to select all path)
//            status_request[3] = 0x00; // Motor ID (Use 0 to select all motor)
//            status_request[4] = 0x00; // Additional data or checksum 

//            // Send the command to the motor
//            Console.WriteLine("Sending command to LSM motor...");
//            // Write the command bytes to the network stream
//            stream.Write(status_request, 0, status_request.Length);

//            // Create a list to store the full response
//            List<byte> fullResponse = new List<byte>();

//            // Buffer to store incoming bytes
//            byte[] buffer = new byte[256];

//            try
//            {
//                int bytesRead;
//                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
//                {
//                    // Add the received bytes to the full response
//                    fullResponse.AddRange(buffer.Take(bytesRead));

//                    // Check if all data has been received
//                    if (!stream.DataAvailable)
//                    {
//                        break;
//                    }
//                }

//                byte[] responseBytes = fullResponse.ToArray();

//                Console.WriteLine(BitConverter.ToString(responseBytes));



//                // Receive the response
//                byte[] response = ReceiveResponse(stream);
//            }
//            catch (IOException ex)
//            {
//                // Handle read timeout or other IO exceptions
//                Console.WriteLine($"IOException: {ex.Message}");
//            }

//            // TEMP ================================================
//            // Process the response
//            //if (response != null)
//            //{
//            //    ParseResponse(response);
//            //}

//            //// Close the stream and client
//            //stream.Close();
//            //client.Close();
//            //Console.WriteLine("Connection Closed");
//        }
//        catch (SocketException se)
//        {
//            Console.WriteLine($"SocketException: {se.SocketErrorCode} - {se.Message}");
//        }
//        catch (TimeoutException te)
//        {
//            Console.WriteLine($"TimeoutException: {te.Message}");
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine($"Exception: {e.Message}");
//        }

//        Console.WriteLine("\nPress Enter to exit...");
//        Console.ReadLine();
//    }

//    static byte[] ConstructMessage(byte messageType, byte[] data)
//    {
//        // Start Bytes
//        byte[] startBytes = new byte[] { 0xAB, 0xBA };

//        // Calculate Message Length
//        // Length = Size of Message Type (1) + Size of Data + Size of CRC (2)
//        int messageLength = 1 + data.Length + 2;
//        if (messageLength < 3 || messageLength > 61)
//        {
//            throw new ArgumentException("Invalid message length. Must be between 3 and 61 bytes.");
//        }

//        // Message Length byte (1 byte)
//        byte lengthByte = (byte)messageLength;

//        // Calculate CRC over Message Type and Data
//        byte[] crc = CalculateCRC(new byte[] { messageType }.Concat(data).ToArray());

//        // Construct the full message
//        List<byte> message = new List<byte>();
//        message.AddRange(startBytes);
//        message.Add(lengthByte);
//        message.Add(messageType);
//        message.AddRange(data);
//        message.AddRange(crc);

//        // Display the message bytes
//        Console.WriteLine($"Constructed Message: {BitConverter.ToString(message.ToArray())}");

//        return message.ToArray();
//    }

//    static byte[] CalculateCRC(byte[] data)
//    {
//        ushort crc = 0xFFFF; // Initial value

//        foreach (byte b in data)
//        {
//            crc ^= (ushort)(b << 8); // Align byte with MSB of CRC

//            for (int i = 0; i < 8; i++)
//            {
//                if ((crc & 0x8000) != 0)
//                {
//                    crc = (ushort)((crc << 1) ^ 0x1021);
//                }
//                else
//                {
//                    crc <<= 1;
//                }
//            }
//        }

//        // Return CRC as two bytes (big-endian)
//        byte crcHigh = (byte)((crc >> 8) & 0xFF);
//        byte crcLow = (byte)(crc & 0xFF);
//        return new byte[] { crcHigh, crcLow };
//    }

//    static byte[] ReceiveResponse(NetworkStream stream)
//    {
//        List<byte> response = new List<byte>();
//        byte[] buffer = new byte[256];

//        try
//        {
//            // Read Start Bytes
//            int bytesRead = stream.Read(buffer, 0, 10);
//            Console.WriteLine(bytesRead);
//            if (bytesRead != 2 || buffer[0] != 0xAB || buffer[1] != 0xBA)
//            {
//                Console.WriteLine("Invalid start bytes in response.");
//                return null;
//            }
//            response.AddRange(buffer.Take(2));

//            // Read Length Byte
//            bytesRead = stream.Read(buffer, 0, 1);
//            if (bytesRead != 1)
//            {
//                Console.WriteLine("Failed to read length byte.");
//                return null;
//            }
//            byte lengthByte = buffer[0];
//            response.Add(lengthByte);

//            // Read the rest of the message
//            int remainingBytes = lengthByte;
//            while (remainingBytes > 0)
//            {
//                bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, remainingBytes));
//                if (bytesRead == 0)
//                {
//                    Console.WriteLine("Connection closed by remote host.");
//                    return null;
//                }
//                response.AddRange(buffer.Take(bytesRead));
//                remainingBytes -= bytesRead;
//            }

//            // Display the response bytes
//            Console.WriteLine($"Received Response: {BitConverter.ToString(response.ToArray())}");

//            return response.ToArray();
//        }
//        catch (IOException ex)
//        {
//            Console.WriteLine($"IOException: {ex.Message}");
//            return null;
//        }
//    }

//    static void ParseResponse(byte[] response)
//    {
//        // Verify Start Bytes
//        if (response[0] != 0xAB || response[1] != 0xBA)
//        {
//            Console.WriteLine("Invalid start bytes in response.");
//            return;
//        }

//        // Get Length Byte
//        byte lengthByte = response[2];
//        int expectedLength = lengthByte + 3; // Length of message including start bytes and length byte

//        if (response.Length != expectedLength)
//        {
//            Console.WriteLine("Response length does not match length byte.");
//            return;
//        }

//        // Extract Message Type, Data, and CRC
//        byte messageType = response[3];
//        int dataLength = lengthByte - 1 - 2; // Subtract Message Type and CRC size
//        byte[] data = new byte[dataLength];
//        Array.Copy(response, 4, data, 0, dataLength);

//        byte crcHigh = response[response.Length - 2];
//        byte crcLow = response[response.Length - 1];
//        byte[] receivedCrc = new byte[] { crcHigh, crcLow };

//        // Calculate CRC over Message Type and Data
//        byte[] calculatedCrc = CalculateCRC(new byte[] { messageType }.Concat(data).ToArray());

//        // Verify CRC
//        if (!receivedCrc.SequenceEqual(calculatedCrc))
//        {
//            Console.WriteLine("CRC check failed.");
//            return;
//        }

//        // Display Parsed Information
//        Console.WriteLine("Response Parsed Successfully:");
//        Console.WriteLine($"  Message Type: {messageType}");
//        Console.WriteLine($"  Data: {BitConverter.ToString(data)}");

//        // Further processing based on message type
//        ProcessResponseData(messageType, data);
//    }

//    static void ProcessResponseData(byte messageType, byte[] data)
//    {
//        // Implement specific parsing based on the message type
//        // For example, if messageType == 0xD7, parse the motor status

//        if (messageType == 0xD7)
//        {
//            // Assuming data format as per previous messages
//            int messageLength = 17; // Each motor status message is 17 bytes long

//            int offset = 0;
//            while (offset + messageLength <= data.Length)
//            {
//                byte[] message = new byte[messageLength];
//                Array.Copy(data, offset, message, 0, messageLength);
//                ParseMotorStatusMessage(message);
//                offset += messageLength;
//            }
//        }
//        else
//        {
//            Console.WriteLine("Unknown message type.");
//        }
//    }

//    static void ParseMotorStatusMessage(byte[] message)
//    {
//        if (message.Length != 17)
//        {
//            Console.WriteLine("Invalid motor status message length.");
//            return;
//        }

//        // Parse the message
//        // Bytes 0-1: Path ID (big-endian)
//        ushort pathId = (ushort)((message[0] << 8) | message[1]);

//        // Byte 2: Path Status
//        byte pathStatus = message[2];

//        // Byte 3: Motor Position on Path
//        byte motorPosition = message[3];

//        // Byte 4: Motor Presence
//        byte motorPresence = message[4];

//        // Byte 5: Motor Type
//        byte motorType = message[5];

//        // Bytes 6-15: Fault Data
//        byte[] faultData = new byte[10];
//        Array.Copy(message, 6, faultData, 0, 10);

//        // Display the parsed information
//        Console.WriteLine($"Motor Status Message:");
//        Console.WriteLine($"  Path ID: {pathId}");
//        Console.WriteLine($"  Path Status: {pathStatus}");
//        Console.WriteLine($"  Motor Position: {motorPosition}");
//        Console.WriteLine($"  Motor Presence: {motorPresence}");
//        Console.WriteLine($"  Motor Type: {motorType}");
//        Console.WriteLine($"  Fault Data: {BitConverter.ToString(faultData)}");
//    }
//}
