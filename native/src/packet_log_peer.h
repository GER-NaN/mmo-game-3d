#pragma once

#include <godot_cpp/classes/e_net_multiplayer_peer.hpp>
#include <godot_cpp/classes/multiplayer_peer_extension.hpp>
#include <godot_cpp/variant/packed_byte_array.hpp>

#include <cstdint>
#include <vector>

namespace godot {

// The server's ENet peer, wrapped so every packet in and out is recorded, replication
// included. Everything the engine calls stays native: replication asks the peer for its
// id once per synchronizer per client per frame, which through C# cost more than the
// rest of the server together.
//
// The records wait in a buffer here; C# takes them once per frame with drain(), so the
// logging costs one call into C# per frame, not one per packet.
class PacketLogPeer : public MultiplayerPeerExtension {
	GDCLASS(PacketLogPeer, MultiplayerPeerExtension)

public:
	// One record: direction (0 in, 1 out), peer, channel, transfer mode, payload size,
	// then the payload. Little-endian, as drain() hands it over.
	static constexpr int RECORD_HEADER_SIZE = 1 + 4 + 1 + 1 + 4;

	// A frame's records past this are dropped and counted, so a stalled drain cannot
	// grow the buffer without bound. Placeholder size.
	static constexpr size_t MAX_BUFFER_BYTES = 16 * 1024 * 1024;

	void wrap(const Ref<ENetMultiplayerPeer> &p_inner);
	PackedByteArray drain();
	int64_t take_dropped();

	Error _get_packet(const uint8_t **r_buffer, int32_t *r_buffer_size) override;
	Error _put_packet(const uint8_t *p_buffer, int32_t p_buffer_size) override;
	int32_t _get_available_packet_count() const override;
	int32_t _get_max_packet_size() const override;
	int32_t _get_packet_channel() const override;
	MultiplayerPeer::TransferMode _get_packet_mode() const override;
	void _set_transfer_channel(int32_t p_channel) override;
	int32_t _get_transfer_channel() const override;
	void _set_transfer_mode(MultiplayerPeer::TransferMode p_mode) override;
	MultiplayerPeer::TransferMode _get_transfer_mode() const override;
	void _set_target_peer(int32_t p_peer) override;
	int32_t _get_packet_peer() const override;
	bool _is_server() const override;
	void _poll() override;
	void _close() override;
	void _disconnect_peer(int32_t p_peer, bool p_force) override;
	int32_t _get_unique_id() const override;
	void _set_refuse_new_connections(bool p_enable) override;
	bool _is_refusing_new_connections() const override;
	bool _is_server_relay_supported() const override;
	MultiplayerPeer::ConnectionStatus _get_connection_status() const override;

protected:
	static void _bind_methods();

private:
	Ref<ENetMultiplayerPeer> inner;

	// The packet last handed to the engine: its bytes must live until the next one.
	PackedByteArray current;

	std::vector<uint8_t> buffer;
	int64_t dropped = 0;
	int32_t target_peer = 0;
	int32_t transfer_channel = 0;
	MultiplayerPeer::TransferMode transfer_mode = MultiplayerPeer::TRANSFER_MODE_RELIABLE;

	void record(uint8_t p_direction, int32_t p_peer, int32_t p_channel, MultiplayerPeer::TransferMode p_mode, const uint8_t *p_data, int32_t p_size);
	void on_peer_connected(int64_t p_id);
	void on_peer_disconnected(int64_t p_id);
};

} // namespace godot
