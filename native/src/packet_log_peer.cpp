#include "packet_log_peer.h"

#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/variant/callable_method_pointer.hpp>

#include <cstring>

namespace godot {

namespace {

// What ENet's multiplayer peer allows.
constexpr int32_t MAX_PACKET_SIZE = 1 << 24;

void put_i32(std::vector<uint8_t> &p_out, int32_t p_value) {
	uint32_t value = static_cast<uint32_t>(p_value);
	p_out.push_back(static_cast<uint8_t>(value));
	p_out.push_back(static_cast<uint8_t>(value >> 8));
	p_out.push_back(static_cast<uint8_t>(value >> 16));
	p_out.push_back(static_cast<uint8_t>(value >> 24));
}

} // namespace

void PacketLogPeer::_bind_methods() {
	ClassDB::bind_method(D_METHOD("wrap", "inner"), &PacketLogPeer::wrap);
	ClassDB::bind_method(D_METHOD("drain"), &PacketLogPeer::drain);
	ClassDB::bind_method(D_METHOD("take_dropped"), &PacketLogPeer::take_dropped);
}

void PacketLogPeer::wrap(const Ref<ENetMultiplayerPeer> &p_inner) {
	inner = p_inner;
	inner->connect("peer_connected", callable_mp(this, &PacketLogPeer::on_peer_connected));
	inner->connect("peer_disconnected", callable_mp(this, &PacketLogPeer::on_peer_disconnected));
}

PackedByteArray PacketLogPeer::drain() {
	PackedByteArray out;
	out.resize(static_cast<int64_t>(buffer.size()));

	if (!buffer.empty()) {
		std::memcpy(out.ptrw(), buffer.data(), buffer.size());
		buffer.clear();
	}

	return out;
}

int64_t PacketLogPeer::take_dropped() {
	int64_t count = dropped;
	dropped = 0;
	return count;
}

void PacketLogPeer::record(uint8_t p_direction, int32_t p_peer, int32_t p_channel, MultiplayerPeer::TransferMode p_mode, const uint8_t *p_data, int32_t p_size) {
	if (buffer.size() + RECORD_HEADER_SIZE + static_cast<size_t>(p_size) > MAX_BUFFER_BYTES) {
		dropped++;
		return;
	}

	buffer.push_back(p_direction);
	put_i32(buffer, p_peer);
	buffer.push_back(static_cast<uint8_t>(p_channel));
	buffer.push_back(static_cast<uint8_t>(p_mode));
	put_i32(buffer, p_size);
	buffer.insert(buffer.end(), p_data, p_data + p_size);
}

// The peer, channel and mode belong to the packet at the front of the queue, so they are
// read before the packet is taken off it.
Error PacketLogPeer::_get_packet(const uint8_t **r_buffer, int32_t *r_buffer_size) {
	int32_t peer = inner->get_packet_peer();
	int32_t channel = inner->get_packet_channel();
	MultiplayerPeer::TransferMode mode = inner->get_packet_mode();
	current = inner->get_packet();

	if (current.is_empty()) {
		*r_buffer = nullptr;
		*r_buffer_size = 0;
		return inner->get_packet_error();
	}

	*r_buffer = current.ptr();
	*r_buffer_size = static_cast<int32_t>(current.size());
	record(0, peer, channel, mode, current.ptr(), *r_buffer_size);
	return OK;
}

Error PacketLogPeer::_put_packet(const uint8_t *p_buffer, int32_t p_buffer_size) {
	PackedByteArray packet;
	packet.resize(p_buffer_size);
	std::memcpy(packet.ptrw(), p_buffer, static_cast<size_t>(p_buffer_size));
	Error error = inner->put_packet(packet);
	record(1, target_peer, transfer_channel, transfer_mode, p_buffer, p_buffer_size);
	return error;
}

int32_t PacketLogPeer::_get_available_packet_count() const {
	return inner->get_available_packet_count();
}

int32_t PacketLogPeer::_get_max_packet_size() const {
	return MAX_PACKET_SIZE;
}

int32_t PacketLogPeer::_get_packet_channel() const {
	return inner->get_packet_channel();
}

MultiplayerPeer::TransferMode PacketLogPeer::_get_packet_mode() const {
	return inner->get_packet_mode();
}

void PacketLogPeer::_set_transfer_channel(int32_t p_channel) {
	transfer_channel = p_channel;
	inner->set_transfer_channel(p_channel);
}

int32_t PacketLogPeer::_get_transfer_channel() const {
	return transfer_channel;
}

void PacketLogPeer::_set_transfer_mode(MultiplayerPeer::TransferMode p_mode) {
	transfer_mode = p_mode;
	inner->set_transfer_mode(p_mode);
}

MultiplayerPeer::TransferMode PacketLogPeer::_get_transfer_mode() const {
	return transfer_mode;
}

void PacketLogPeer::_set_target_peer(int32_t p_peer) {
	target_peer = p_peer;
	inner->set_target_peer(p_peer);
}

int32_t PacketLogPeer::_get_packet_peer() const {
	return inner->get_packet_peer();
}

bool PacketLogPeer::_is_server() const {
	return inner->get_unique_id() == 1;
}

void PacketLogPeer::_poll() {
	inner->poll();
}

void PacketLogPeer::_close() {
	inner->close();
}

void PacketLogPeer::_disconnect_peer(int32_t p_peer, bool p_force) {
	inner->disconnect_peer(p_peer, p_force);
}

int32_t PacketLogPeer::_get_unique_id() const {
	return inner->get_unique_id();
}

void PacketLogPeer::_set_refuse_new_connections(bool p_enable) {
	inner->set_refuse_new_connections(p_enable);
}

bool PacketLogPeer::_is_refusing_new_connections() const {
	return inner->is_refusing_new_connections();
}

bool PacketLogPeer::_is_server_relay_supported() const {
	return inner->is_server_relay_supported();
}

MultiplayerPeer::ConnectionStatus PacketLogPeer::_get_connection_status() const {
	return inner->get_connection_status();
}

void PacketLogPeer::on_peer_connected(int64_t p_id) {
	emit_signal("peer_connected", p_id);
}

void PacketLogPeer::on_peer_disconnected(int64_t p_id) {
	emit_signal("peer_disconnected", p_id);
}

} // namespace godot
