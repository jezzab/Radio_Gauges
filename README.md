Display gauges in RGBS format to be viewed on a compatible radio (Holden VE IQ, S1, Ford ICC) using data from HSCAN

CAN Bus Setup
-------------
CAN1 is GMLAN
CAN2 is HSCAN

RGBS Setup
----------
Composite Sync is remapped to the Horizontal Sync pin on the Chrontel 7026B IC withthe driver and using XOR multiplexing.
May require ~15 Ohm resistor between Composite Sync and Ground for IQ display

Navigation enable packets are sent over GMLAN when the IsS1 bool is true
