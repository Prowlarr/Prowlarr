import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import { icons } from 'Helpers/Props';
import DeleteMovieModal from 'Indexer/Delete/DeleteMovieModal';
import EditMovieModalConnector from 'Indexer/Edit/EditMovieModalConnector';
import translate from 'Utilities/String/translate';

class IndexerIndexActionsCell extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditMovieModalOpen: false,
      isDeleteMovieModalOpen: false
    };
  }

  //
  // Listeners

  onEditMoviePress = () => {
    this.setState({ isEditMovieModalOpen: true });
  };

  onEditMovieModalClose = () => {
    this.setState({ isEditMovieModalOpen: false });
  };

  onDeleteMoviePress = () => {
    this.setState({
      isEditMovieModalOpen: false,
      isDeleteMovieModalOpen: true
    });
  };

  onDeleteMovieModalClose = () => {
    this.setState({ isDeleteMovieModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      id,
      isRefreshingMovie,
      onRefreshMoviePress,
      ...otherProps
    } = this.props;

    const {
      isEditMovieModalOpen,
      isDeleteMovieModalOpen
    } = this.state;

    return (
      <VirtualTableRowCell
        {...otherProps}
      >
        <SpinnerIconButton
          name={icons.REFRESH}
          title={translate('RefreshMovie')}
          isSpinning={isRefreshingMovie}
          onPress={onRefreshMoviePress}
        />

        <IconButton
          name={icons.EDIT}
          title={translate('EditIndexer')}
          onPress={this.onEditMoviePress}
        />

        <EditMovieModalConnector
          isOpen={isEditMovieModalOpen}
          indexerId={id}
          onModalClose={this.onEditMovieModalClose}
          onDeleteMoviePress={this.onDeleteMoviePress}
        />

        <DeleteMovieModal
          isOpen={isDeleteMovieModalOpen}
          indexerId={id}
          onModalClose={this.onDeleteMovieModalClose}
        />
      </VirtualTableRowCell>
    );
  }
}

IndexerIndexActionsCell.propTypes = {
  id: PropTypes.number.isRequired,
  isRefreshingMovie: PropTypes.bool.isRequired,
  onRefreshMoviePress: PropTypes.func.isRequired
};

export default IndexerIndexActionsCell;
